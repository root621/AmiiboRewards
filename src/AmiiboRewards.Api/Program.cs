using AmiiboRewards.Application;
using AmiiboRewards.Domain;
using AmiiboRewards.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("AmiiboRewards") ?? "Host=localhost;Port=5432;Database=amiibo_rewards;Username=amiibo;Password=amiibo";
builder.Services.AddDbContext<AmiiboRewardsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IRewardSearchService, EfRewardSearchService>();
builder.Services.AddScoped<GameLibrary>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/games", async (GameLibrary library, IWebHostEnvironment environment, CancellationToken ct) =>
    Results.Ok(await library.ListAsync(AssetsRoot(environment), ct)));
app.MapPost("/api/dumps/scan", async (GameLibrary library, AmiiboRewardsDbContext db, IConfiguration configuration, IWebHostEnvironment environment, CancellationToken ct) =>
{
    var root = await GetDumpsRootAsync(environment, configuration, db, ct);
    var warnings = await library.ScanAsync(root, AssetsRoot(environment), ct);
    return Results.Ok(new { path = root, games = await library.ListAsync(AssetsRoot(environment), ct), warnings });
});
app.MapGet("/api/dumps/config", async (IWebHostEnvironment environment, IConfiguration configuration, AmiiboRewardsDbContext db, CancellationToken ct) =>
{
    var configured = await GetDumpsRootAsync(environment, configuration, db, ct);
    return Results.Ok(new { path = configured });
});
app.MapPut("/api/dumps/config", async (DumpConfigRequest request, IWebHostEnvironment environment, IConfiguration configuration, AmiiboRewardsDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Path)) return Results.BadRequest(new { error = "La ruta no puede estar vacía." });
    string path;
    try { path = Path.GetFullPath(request.Path, environment.ContentRootPath); }
    catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return Results.BadRequest(new { error = "La ruta indicada no es válida." }); }
    if (!Directory.Exists(path)) return Results.BadRequest(new { error = "La carpeta indicada no existe." });
    await db.Database.MigrateAsync(ct);
    var setting = await db.AppSettings.SingleOrDefaultAsync(x => x.Key == "DumpsRootPath", ct);
    if (setting is null) db.AppSettings.Add(new AppSetting { Key = "DumpsRootPath", Value = path });
    else { setting.Value = path; setting.UpdatedAt = DateTimeOffset.UtcNow; }
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { path });
});
app.MapGet("/api/dumps/games", async (IWebHostEnvironment environment, IConfiguration configuration, AmiiboRewardsDbContext db, CancellationToken ct) =>
{
    var root = await GetDumpsRootAsync(environment, configuration, db, ct);
    return Results.Ok(GameDefinitions.Scan(root).Detected.Select(d => new { code = d.Game.Code, name = d.Game.Name, path = d.RomFsPath, romFsPath = d.RomFsPath, supported = d.Game.CanImport }));
});
app.MapPost("/api/dumps/import", async (DumpImportRequest request, IWebHostEnvironment environment, IConfiguration configuration, AmiiboRewardsDbContext db, CancellationToken ct) =>
{
    var code = request.Code.ToUpperInvariant();
    if (GameDefinitions.Find(code)?.CanImport != true || (code == "BOTW" && !System.Text.RegularExpressions.Regex.IsMatch(request.Locale ?? "", "^[A-Za-z]{4}$"))) return Results.BadRequest(new { error = "No hay un importador disponible para ese juego o locale." });
    var root = await GetDumpsRootAsync(environment, configuration, db, ct);
    var registered = await db.Games.AsNoTracking().SingleOrDefaultAsync(g => g.Code == code, ct);
    var candidates = GameDefinitions.Scan(root).Detected.Where(d => d.Game.Code == code).ToList();
    var dump = (candidates.FirstOrDefault(d => d.RomFsPath == registered?.RomFsPath) ?? candidates.FirstOrDefault())?.RomFsPath;
    if (dump is null) return Results.NotFound(new { error = $"No se encontró una RomFS de {code} en la carpeta dumps." });
    var repoRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", ".."));
    var project = Path.Combine(repoRoot, "tools", "AmiiboRewards.Tools.Botw", "AmiiboRewards.Tools.Botw.csproj");
    if (!File.Exists(project)) return Results.Problem("No se encontró el importador BOTW configurado.");
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = repoRoot, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
    var command = code == "TOTK" ? new[] { "run", "--project", project, "--", "import-totk-romfs" } : new[] { "run", "--project", project, "--", "import-romfs", request.Locale! };
    foreach (var argument in command) start.ArgumentList.Add(argument);
    start.Environment[code == "TOTK" ? "AMIIBOREWARDS_Totk__RomFsPath" : "AMIIBOREWARDS_Botw__RomFsPath"] = dump;
    start.Environment["AMIIBOREWARDS_ConnectionStrings__AmiiboRewards"] = configuration.GetConnectionString("AmiiboRewards") ?? "Host=localhost;Port=5432;Database=amiibo_rewards;Username=amiibo;Password=amiibo";
    var process = Process.Start(start);
    if (process is null) return Results.Problem("No se pudo iniciar la importación.");
    _ = Task.Run(async () => { await Task.WhenAll(process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync()); await process.WaitForExitAsync(); process.Dispose(); });
    return Results.Accepted("/api/admin/imports", new { status = "started", game = request.Code, locale = request.Locale });
});
app.MapGet("/api/locales", async (string? game, AmiiboRewardsDbContext db, CancellationToken ct) =>
    Results.Ok(await db.RewardLocalizations.AsNoTracking().Where(x => x.Reward!.Game!.Code == (game ?? "BOTW").ToUpperInvariant()).GroupBy(x => x.Locale).OrderBy(x => x.Key)
        .Select(x => new { locale = x.Key, localizedRewards = x.Select(y => y.RewardId).Distinct().Count() }).ToListAsync(ct)));
app.MapGet("/api/rewards/search", async (string? q, string? locale, string? game, IRewardSearchService search, CancellationToken ct) =>
{
    var selectedLocale = locale is { Length: 4 } ? locale : "USes";
    return Results.Ok(await search.SearchAsync(q ?? string.Empty, selectedLocale, ct, (game ?? "BOTW").ToUpperInvariant()));
});
app.MapGet("/api/rewards/catalog", async (string? locale, string? game, AmiiboRewardsDbContext db, CancellationToken ct) =>
{
    var selectedLocale = locale is { Length: 4 } ? locale : "USes";
    var catalog = AmiiboAssociationExpansion.Deduplicate(await db.AmiiboCatalog.AsNoTracking().ToListAsync(ct));
    var rewards = await db.Rewards.AsNoTracking().Where(r => r.Game!.Code == (game ?? "BOTW").ToUpperInvariant()).OrderBy(r => r.InternalId).Select(r => new
    {
        r.InternalId,
        Name = r.Localizations.Where(l => l.Locale == selectedLocale).Select(l => l.Name).FirstOrDefault() ?? r.Name,
        Description = r.Localizations.Where(l => l.Locale == selectedLocale).Select(l => l.Description).FirstOrDefault() ?? r.Description,
        IconPath = r.IconAsset == null ? null : r.IconAsset.OutputPath,
        AmiiboRewards = r.AmiiboRewards.Select(ar => new
        {
            ar.Id,
            ar.Amiibo!.InternalTableId,
            ar.Amiibo.Name,
            ar.Amiibo.MappingStatus,
            ar.Pool,
            ar.Probability,
            ar.RawWeight,
            ar.MinCount,
            ar.MaxCount,
            ar.Condition,
            ar.InteractionKind,
            ar.OutcomeKind,
            ar.SpecialMetadata
        }).ToList()
    }).ToListAsync(ct);

    var projected = rewards.Select(r =>
    {
        var selectors = r.AmiiboRewards.Select(ar => (ar.Id, AmiiboAssociationExpansion.ParseSelector(ar.InternalTableId).Kind, AmiiboAssociationExpansion.ParseSelector(ar.InternalTableId).Value)).ToList();
        var expansions = AmiiboAssociationExpansion.ExpandGroup(selectors, catalog);
        var amiibo = r.AmiiboRewards.SelectMany(ar =>
        {
            var (kind, value) = AmiiboAssociationExpansion.ParseSelector(ar.InternalTableId);
            var matches = expansions[ar.Id];
            var selectorKindLabel = kind switch { TotkSelectorKind.NumberingId => "NumberingId", TotkSelectorKind.CharacterId => "CharacterId", TotkSelectorKind.CharacterBaseId => "CharacterBaseId", _ => "Default" };
            IReadOnlyList<string> names = matches.Count > 0
                ? matches.Select(m => m.DisplayName).ToList()
                : [FallbackDisplayName(kind, value, ar.Name)];
            return names.Select(name => new
            {
                AmiiboDisplayName = name,
                SelectorKind = selectorKindLabel,
                SelectorValue = value,
                ar.MappingStatus,
                ar.Pool,
                ar.Probability,
                NormalizedProbability = ar.Probability,
                ar.RawWeight,
                ar.MinCount,
                ar.MaxCount,
                ar.Condition,
                IsConditional = ar.Condition != null,
                ar.InteractionKind,
                ar.OutcomeKind,
                IsSpecial = ar.InteractionKind == InteractionKind.Special || ar.OutcomeKind == OutcomeKind.Special,
                ar.SpecialMetadata
            });
        }).ToList();
        return new { r.InternalId, r.Name, r.Description, r.IconPath, AmiiboCount = amiibo.Select(x => x.AmiiboDisplayName).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Amiibo = amiibo };
    }).ToList();

    return Results.Ok(projected.Select(r => new { r.InternalId, r.Name, r.Description, r.IconPath, r.AmiiboCount, category = CatalogCategory(r.InternalId), r.Amiibo }));
});
app.MapGet("/api/admin/imports", async (string? game, HttpRequest request, AmiiboRewardsDbContext db, IConfiguration configuration, CancellationToken ct) =>
{
    var key = configuration["Admin:Key"]; if (!string.IsNullOrEmpty(key) && request.Headers["X-Admin-Key"] != key) return Results.Unauthorized();
    var runs = await db.ImportRuns.AsNoTracking().Where(x => x.Game!.Code == (game ?? "BOTW").ToUpperInvariant()).OrderByDescending(x => x.StartedAt).Take(20).Select(x => new { x.Id, x.Kind, x.Status, x.StartedAt, x.FinishedAt, x.Summary }).ToListAsync(ct); return Results.Ok(runs);
});
app.MapGet("/api/assets/{game}/{asset}", (string game, string asset, IWebHostEnvironment environment) =>
{
    if (!System.Text.RegularExpressions.Regex.IsMatch(game, "^[a-zA-Z0-9_-]+$") || !System.Text.RegularExpressions.Regex.IsMatch(asset, "^[a-zA-Z0-9_-]+$")) return Results.BadRequest();
    var root = Path.Combine(AssetsRoot(environment), game.ToLowerInvariant());
    if (asset.Equals("cover", StringComparison.OrdinalIgnoreCase)) { var cover = Path.Combine(root, "cover.jpg"); return File.Exists(cover) ? Results.File(cover, "image/jpeg") : Results.NotFound(); }
    var file = Path.Combine(root, "icons", asset + ".png"); return File.Exists(file) ? Results.File(file, "image/png") : Results.NotFound();
});
app.Run();
static string CatalogCategory(string id) => id switch
{
    _ when id.StartsWith("Weapon_Bow_", StringComparison.Ordinal) => "Arcos",
    _ when id.StartsWith("Weapon_Shield_", StringComparison.Ordinal) => "Escudos",
    _ when id.StartsWith("Weapon_Sword_", StringComparison.Ordinal) || id.StartsWith("Weapon_Lsword_", StringComparison.Ordinal) || id.StartsWith("Weapon_Spear_", StringComparison.Ordinal) => "Armas",
    _ when id.StartsWith("Armor_", StringComparison.Ordinal) => "Armaduras",
    _ when id.StartsWith("Item_Meat_", StringComparison.Ordinal) || id.StartsWith("Item_Fruit_", StringComparison.Ordinal) || id.StartsWith("Item_Mushroom", StringComparison.Ordinal) || id.StartsWith("Item_Roast_", StringComparison.Ordinal) || id.StartsWith("Item_FishGet_", StringComparison.Ordinal) || id.StartsWith("Animal_Fish_", StringComparison.Ordinal) => "Comida y fauna",
    _ when id.StartsWith("Obj_", StringComparison.Ordinal) => "Flechas y objetos",
    _ when id.StartsWith("Item_", StringComparison.Ordinal) => "Materiales",
    _ => "Otros"
};
static string AssetsRoot(IWebHostEnvironment environment) => Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "assets"));
static string FallbackDisplayName(TotkSelectorKind? kind, string value, string? amiiboName)
{
    // Only reached when the local catalog has no concrete amiibo for this selector.
    if (kind == TotkSelectorKind.NumberingId) return $"Amiibo #{value}";
    if (kind == TotkSelectorKind.CharacterId) return TotkAmiiboRuleMatcher.FamilyDisplayNames.GetValueOrDefault(value, value);
    if (kind == TotkSelectorKind.CharacterBaseId) return $"Familia {TotkAmiiboRuleMatcher.FamilyDisplayNames.GetValueOrDefault(value, value)}";
    if (amiiboName != null && !amiiboName.StartsWith("Amiibo #") && !amiiboName.StartsWith("Familia ") && !amiiboName.StartsWith("Amiibo ")) return amiiboName;
    return "Amiibo sin identificar";
}
static async Task<string> GetDumpsRootAsync(IWebHostEnvironment environment, IConfiguration configuration, AmiiboRewardsDbContext db, CancellationToken ct)
{
    var setting = await db.AppSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == "DumpsRootPath", ct);
    var configured = setting?.Value ?? configuration["DumpsRootPath"] ?? "dumps";
    return Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured));
}
public sealed record DumpImportRequest(string Code, string? Locale);
public sealed record DumpConfigRequest(string Path);
public partial class Program { }
