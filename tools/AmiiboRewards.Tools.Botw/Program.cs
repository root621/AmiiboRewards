using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiiboRewards.Domain;
using AmiiboRewards.Infrastructure;
using AmiiboRewards.Tools.Botw;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

if (args.Length < 1) { Console.WriteLine("Commands: import-romfs [locale], import-totk-romfs [locale], import-amiibo-bins <directory>, diagnose-totk-selectors <directory>, inspect-totk-romfs, inspect-totk-localization <locale>, analyze-romfs --romfs <path>, import-json <enriched-drops.json>, import-amiibo-catalog <catalog.json>, import-localization <locale>, unresolved-localization <locale>, find-localization <locale> <fragment>, inspect-localization <locale> <label>, inspect-pack <sbactorpack>, extract-bdrop <sbactorpack> <entry> <output>, parse-aamp <bdrop>, validate"); return 0; }
var connection = Environment.GetEnvironmentVariable("AMIIBOREWARDS_ConnectionStrings__AmiiboRewards") ?? "Host=localhost;Port=5432;Database=amiibo_rewards;Username=amiibo;Password=amiibo";
var options = new DbContextOptionsBuilder<AmiiboRewardsDbContext>().UseNpgsql(connection).Options;
using var lf = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true)); var log = lf.CreateLogger("Botw"); await using var db = new AmiiboRewardsDbContext(options);
if (args[0] == "validate") return await ValidateAsync(db, log);
if (args[0] == "unresolved-localization" && args.Length == 2) { foreach (var id in await db.Rewards.Where(r => !r.Localizations.Any(l => l.Locale == args[1])).OrderBy(r => r.InternalId).Select(r => r.InternalId).ToListAsync()) Console.WriteLine(id); return 0; }
if (args[0] == "import-amiibo-catalog" && args.Length == 2) { if (!File.Exists(args[1])) { Console.Error.WriteLine("Catalog file not found."); return 2; } await db.Database.MigrateAsync(); var catalogGame = await db.Games.SingleOrDefaultAsync(x => x.Code == "BOTW"); if (catalogGame is null) { catalogGame = new Game { Code = "BOTW", Name = "The Legend of Zelda: Breath of the Wild" }; db.Games.Add(catalogGame); await db.SaveChangesAsync(); } var catalog = JsonSerializer.Deserialize<List<AmiiboCatalogEntry>>(await File.ReadAllTextAsync(args[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } }) ?? []; foreach (var item in catalog) { var source = item.MappingSource ?? (item.MappingStatus == AmiiboMappingStatus.Confirmed ? "MrCheeze/botw-tools droplists_amiibo.py" : "BOTW drop-table catalog"); var amiibo = await db.Amiibo.SingleOrDefaultAsync(x => x.GameId == catalogGame.Id && x.InternalTableId == item.TableId); if (amiibo is null) db.Amiibo.Add(new Amiibo { GameId = catalogGame.Id, InternalTableId = item.TableId, Name = item.Name, MappingStatus = item.MappingStatus, MappingSource = source }); else { amiibo.Name = item.Name; amiibo.MappingStatus = item.MappingStatus; amiibo.MappingSource = source; } } await db.SaveChangesAsync(); log.LogInformation("Imported {Count} amiibo mapping records.", catalog.Count); return 0; }
if (args[0] == "import-amiibo-bins" && args.Length == 2) return await ImportAmiiboBinsAsync(args[1], db, log);
if (args[0] == "diagnose-totk-selectors" && args.Length == 2) return await DiagnoseTotkSelectorsAsync(args[1], db);
if (args[0] == "inspect-pack" && args.Length == 2) { var decoded = SwitchArchive.DecodeCompression(await File.ReadAllBytesAsync(args[1])); foreach (var name in SwitchArchive.ReadNames(decoded).Where(x => x.EndsWith(".bdrop", StringComparison.OrdinalIgnoreCase))) Console.WriteLine(name); return 0; }
if (args[0] == "extract-bdrop" && args.Length == 4) { var decoded = SwitchArchive.DecodeCompression(await File.ReadAllBytesAsync(args[1])); await File.WriteAllBytesAsync(args[3], SwitchArchive.Extract(decoded, args[2])); Console.WriteLine(args[3]); return 0; }
if (args[0] == "parse-aamp" && args.Length == 2) { foreach (var pool in AampDropTableReader.ReadDropTable(await File.ReadAllBytesAsync(args[1]))) { Console.WriteLine($"{pool.Name}: {pool.Rewards.Count} rewards"); foreach (var reward in pool.Rewards) Console.WriteLine($"  {reward.Probability:0.##}% {reward.ItemId}"); } return 0; }
if (args[0] == "import-romfs" && args.Length is 1 or 2)
{
    var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Botw__RomFsPath");
    if (string.IsNullOrWhiteSpace(romFs)) { Console.Error.WriteLine("Set AMIIBOREWARDS_Botw__RomFsPath to the RomFS directory."); return 2; }
    return await ImportRomFsAsync(romFs, args.Length == 2 ? args[1] : "USes", db, log);
}
if (args[0] == "import-totk-romfs" && args.Length is 1 or 2)
{
    var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Totk__RomFsPath");
    if (string.IsNullOrWhiteSpace(romFs)) { Console.Error.WriteLine("Set AMIIBOREWARDS_Totk__RomFsPath to the TOTK RomFS directory."); return 2; }
    return await ImportTotkRomFsAsync(romFs, args.Length == 2 ? args[1] : "USen", db, log);
}
if (args[0] == "inspect-totk-romfs" && args.Length == 1)
{
    var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Totk__RomFsPath");
    if (string.IsNullOrWhiteSpace(romFs)) { Console.Error.WriteLine("Set AMIIBOREWARDS_Totk__RomFsPath to the TOTK RomFS directory."); return 2; }
    var data = await TotkRomFsReader.ReadAsync(romFs); Console.WriteLine($"{data.Settings.Rules.Count} selector rules; {data.Settings.DefaultRule.Pools.Count} default pools; {data.Settings.Rules.Sum(x => x.Pools.Sum(p => p.Entries.Count))} entries; {data.SourceSha256}"); return 0;
}
if (args[0] == "inspect-totk-localization" && args.Length is 2 or 3)
{
    var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Totk__RomFsPath"); if (string.IsNullOrWhiteSpace(romFs)) return 2;
    var messages = await TotkMessageCatalogReader.ReadAsync(romFs, args[1]); var matches = args.Length == 3 ? messages.Where(x => x.Key.Contains(args[2], StringComparison.OrdinalIgnoreCase) || x.Value.Contains(args[2], StringComparison.OrdinalIgnoreCase)) : messages.Take(5); Console.WriteLine($"{messages.Count} actor message labels"); foreach (var item in matches.Take(50)) Console.WriteLine($"{item.Key} = {item.Value}"); return 0;
}
if (args[0] == "analyze-romfs" && args.Length is 2 or 3) return AnalyzeRomFs(args.Length == 3 && args[1] == "--romfs" ? args[2] : args[1]);
if (args[0] == "inspect-localization" && args.Length == 3) return await InspectLocalizationAsync(args[1], args[2]);
if (args[0] == "find-localization" && args.Length == 3) return await FindLocalizationAsync(args[1], args[2]);
if (args[0] == "import-localization" && args.Length == 2) return await ImportLocalizationAsync(args[1], db, log);
if (args[0] != "import-json" || args.Length != 2) { Console.Error.WriteLine("Usage: import-json <enriched-drops.json>"); return 2; }
if (!File.Exists(args[1])) { log.LogError("Input not found: {Path}", args[1]); return 2; }
await db.Database.MigrateAsync(); var hash = Convert.ToHexString(await SHA256.HashDataAsync(File.OpenRead(args[1]))).ToLowerInvariant();
var game = await db.Games.SingleOrDefaultAsync(x => x.Code == "BOTW"); if (game is null) { game = new Game { Code = "BOTW", Name = "The Legend of Zelda: Breath of the Wild" }; db.Games.Add(game); await db.SaveChangesAsync(); }
var run = await db.ImportRuns.SingleOrDefaultAsync(x => x.GameId == game.Id && x.Kind == "enriched-json" && x.SourceSha256 == hash); if (run?.Status is ImportStatus.Succeeded or ImportStatus.SucceededWithWarnings) { log.LogInformation("Source already imported: {Hash}", hash); return 0; }
if (run is null) { run = new ImportRun { GameId = game.Id, Kind = "enriched-json", SourcePath = Path.GetFullPath(args[1]), SourceSha256 = hash, Status = ImportStatus.Running }; db.ImportRuns.Add(run); } else { run.Status = ImportStatus.Running; run.Summary = null; run.FinishedAt = null; } await db.SaveChangesAsync();
try { var tables = JsonSerializer.Deserialize<List<DropTable>>(await File.ReadAllTextAsync(args[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Invalid enriched JSON."); var entries = 0; var unresolved = 0;
foreach (var table in tables) { var amiibo = await db.Amiibo.SingleOrDefaultAsync(x => x.GameId == game.Id && x.InternalTableId == table.TableId); if (amiibo is null) { var known = table.TableId == "013" ? "Revali" : null; unresolved += known is null ? 1 : 0; amiibo = new Amiibo { GameId = game.Id, InternalTableId = table.TableId, Name = known }; db.Amiibo.Add(amiibo); await db.SaveChangesAsync(); } var packHash = await SourceHashAsync(table.SourcePack, hash);
foreach (var pool in table.Pools) for (var i = 0; i < pool.Rewards.Count; i++) { var e = pool.Rewards[i]; var reward = await db.Rewards.SingleOrDefaultAsync(x => x.GameId == game.Id && x.InternalId == e.ItemId); if (reward is null) { reward = new Reward { GameId = game.Id, InternalId = e.ItemId, Name = e.Name ?? e.ItemId, Description = e.Description, RewardType = Classify(e.ItemId), NameSourcePath = "Pack/Bootup_USen.pack::Message/Msg_USen.product.ssarc", DescriptionSourcePath = "Pack/Bootup_USen.pack::Message/Msg_USen.product.ssarc", NameSourceSha256 = hash, DescriptionSourceSha256 = hash }; db.Rewards.Add(reward); await db.SaveChangesAsync(); } var rel = await db.AmiiboRewards.SingleOrDefaultAsync(x => x.AmiiboId == amiibo.Id && x.RewardId == reward.Id && x.Pool == pool.Name && x.EntryIndex == i); if (rel is null) db.AmiiboRewards.Add(new AmiiboReward { AmiiboId = amiibo.Id, RewardId = reward.Id, Pool = pool.Name, Probability = e.Probability, Condition = Condition(pool.Name), DropSourcePath = $"Actor/Pack/{table.SourcePack}::{table.SourceBdrop}", DropSourceSha256 = packHash, EntryIndex = i }); else { rel.Probability = e.Probability; rel.DropSourceSha256 = packHash; } entries++; } }
run.Status = unresolved == 0 ? ImportStatus.Succeeded : ImportStatus.SucceededWithWarnings; run.FinishedAt = DateTimeOffset.UtcNow; run.Summary = $"{tables.Count} tables; {entries} entries; {unresolved} unresolved amiibo mappings."; await db.SaveChangesAsync(); log.LogInformation("{Summary}", run.Summary); return 0; }
catch (Exception ex) { run.Status = ImportStatus.Failed; run.FinishedAt = DateTimeOffset.UtcNow; run.Summary = ex.Message; await db.SaveChangesAsync(); log.LogError(ex, "Import failed"); return 1; }

static async Task<int> ValidateAsync(AmiiboRewardsDbContext db, ILogger log) { var u = await db.Amiibo.CountAsync(a => a.Name == null); var n = await db.Rewards.CountAsync(r => r.Name == r.InternalId); var i = await db.Rewards.CountAsync(r => r.IconAssetId == null); log.LogInformation("{Amiibo} unresolved amiibo; {Names} unresolved names; {Icons} icons pending", u, n, i); return n == 0 ? 0 : 1; }
static async Task<int> ImportRomFsAsync(string romFs, string locale, AmiiboRewardsDbContext db, ILogger log)
{
    var tables = await RomFsDropTableReader.ReadAsync(romFs);
    var importHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", tables.Select(x => $"{x.SourcePack}:{x.SourceSha256}"))))).ToLowerInvariant();
    await db.Database.MigrateAsync();
    var game = await db.Games.SingleOrDefaultAsync(x => x.Code == "BOTW");
    if (game is null) { game = new Game { Code = "BOTW", Name = "The Legend of Zelda: Breath of the Wild" }; db.Games.Add(game); await db.SaveChangesAsync(); }
    var run = await db.ImportRuns.SingleOrDefaultAsync(x => x.GameId == game.Id && x.Kind == "romfs-aamp" && x.SourceSha256 == importHash);
    if (run?.Status is ImportStatus.Succeeded or ImportStatus.SucceededWithWarnings) { log.LogInformation("RomFS source already imported: {Hash}", importHash); return await ImportLocalizationAsync(locale, db, log); }
    if (run is null) { run = new ImportRun { GameId = game.Id, Kind = "romfs-aamp", SourcePath = Path.Combine(romFs, "Actor", "Pack"), SourceSha256 = importHash, Status = ImportStatus.Running }; db.ImportRuns.Add(run); }
    else { run.Status = ImportStatus.Running; run.Summary = null; run.FinishedAt = null; }
    await db.SaveChangesAsync();
    try
    {
        var entries = 0; var unresolved = 0;
        foreach (var table in tables)
        {
            var amiibo = await db.Amiibo.SingleOrDefaultAsync(x => x.GameId == game.Id && x.InternalTableId == table.TableId);
            if (amiibo is null) { amiibo = new Amiibo { GameId = game.Id, InternalTableId = table.TableId, Name = null }; db.Amiibo.Add(amiibo); unresolved++; await db.SaveChangesAsync(); }
        var pools = AampDropTableReader.ReadDropTable(table.Bdrop);
            foreach (var pool in pools)
            for (var index = 0; index < pool.Rewards.Count; index++)
            {
                var entry = pool.Rewards[index];
                var reward = await db.Rewards.SingleOrDefaultAsync(x => x.GameId == game.Id && x.InternalId == entry.ItemId);
                if (reward is null)
                {
                    reward = new Reward { GameId = game.Id, InternalId = entry.ItemId, Name = entry.ItemId, RewardType = Classify(entry.ItemId), NameSourcePath = "AAMP pending localization", NameSourceSha256 = table.SourceSha256 };
                    db.Rewards.Add(reward); await db.SaveChangesAsync();
                }
                var relation = await db.AmiiboRewards.SingleOrDefaultAsync(x => x.AmiiboId == amiibo.Id && x.RewardId == reward.Id && x.Pool == pool.Name && x.EntryIndex == index);
                if (relation is null) db.AmiiboRewards.Add(new AmiiboReward { AmiiboId = amiibo.Id, RewardId = reward.Id, Pool = pool.Name, Probability = (decimal)entry.Probability, Condition = Condition(pool.Name), DropSourcePath = $"Actor/Pack/{table.SourcePack}::{table.SourceBdrop}", DropSourceSha256 = table.SourceSha256, EntryIndex = index });
                else { relation.Probability = (decimal)entry.Probability; relation.Condition = Condition(pool.Name); relation.DropSourceSha256 = table.SourceSha256; }
                entries++;
            }
        }
        run.Status = unresolved == 0 ? ImportStatus.Succeeded : ImportStatus.SucceededWithWarnings;
        run.Summary = $"{tables.Count} RomFS tables; {entries} AAMP entries; {unresolved} unresolved amiibo mappings.";
        run.FinishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(); log.LogInformation("{Summary}", run.Summary);
        return await ImportLocalizationAsync(locale, db, log);
    }
    catch (Exception ex) { run.Status = ImportStatus.Failed; run.FinishedAt = DateTimeOffset.UtcNow; run.Summary = ex.Message; await db.SaveChangesAsync(); log.LogError(ex, "RomFS AAMP import failed"); return 1; }
}
static async Task<int> ImportTotkRomFsAsync(string romFs, string locale, AmiiboRewardsDbContext db, ILogger log)
{
    var profile = GameProfiles.Totk; var index = RomFsIndex.Create(romFs); var adapter = new TotkAmiiboInteractionAdapter();
    if (!adapter.Supports(profile, index)) { Console.Error.WriteLine("The path is not a supported TOTK RomFS."); return 2; }
    var interactions = await adapter.ReadAsync(profile, index);
    var hash = interactions.FirstOrDefault()?.SourceHash ?? throw new InvalidDataException("TOTK AmiiboSetting contains no source hash.");
    await db.Database.MigrateAsync();
    var game = await db.Games.SingleOrDefaultAsync(x => x.Code == "TOTK");
    if (game is null) { game = new Game { Code = "TOTK", Name = profile.Name, RomFsPath = index.RootPath, LastDetectedAt = DateTimeOffset.UtcNow }; db.Games.Add(game); await db.SaveChangesAsync(); }
    else { game.RomFsPath = index.RootPath; game.LastDetectedAt = DateTimeOffset.UtcNow; }
    var run = await db.ImportRuns.SingleOrDefaultAsync(x => x.GameId == game.Id && x.Kind == "totk-amiibo-byml" && x.SourceSha256 == hash);
    // Reprocessing is idempotent and lets newly added localization/asset enrichers fill existing rows.
    if (run?.Status is ImportStatus.Succeeded or ImportStatus.SucceededWithWarnings) log.LogInformation("Refreshing TOTK enrichment for source: {Hash}", hash);
    if (run is null) { run = new ImportRun { GameId = game.Id, Kind = "totk-amiibo-byml", SourcePath = $"{TotkRomFsReader.ResidentArchive}::{TotkRomFsReader.SettingsPath}", SourceSha256 = hash, Status = ImportStatus.Running }; db.ImportRuns.Add(run); }
    else { run.Status = ImportStatus.Running; run.FinishedAt = null; run.Summary = null; }
    await db.SaveChangesAsync();
    try
    {
        var count = 0; var unresolved = 0;
        foreach (var interaction in interactions)
        {
            var amiibo = await db.Amiibo.SingleOrDefaultAsync(x => x.GameId == game.Id && x.InternalTableId == interaction.SelectorId);
            if (amiibo is null) { amiibo = new Amiibo { GameId = game.Id, InternalTableId = interaction.SelectorId, Name = SelectorDisplayName(interaction.SelectorId), MappingStatus = AmiiboMappingStatus.Confirmed, MappingSource = "TOTK AmiiboSetting selector" }; db.Amiibo.Add(amiibo); await db.SaveChangesAsync(); }
            var reward = await db.Rewards.SingleOrDefaultAsync(x => x.GameId == game.Id && x.InternalId == interaction.SourceId);
            if (reward is null) { reward = new Reward { GameId = game.Id, InternalId = interaction.SourceId, Name = interaction.SourceId, RewardType = ToRewardType(interaction.OutcomeKind), NameSourcePath = interaction.SourcePath, NameSourceSha256 = interaction.SourceHash, Metadata = interaction.SpecialMetadata }; db.Rewards.Add(reward); await db.SaveChangesAsync(); unresolved++; }
            var relation = await db.AmiiboRewards.SingleOrDefaultAsync(x => x.AmiiboId == amiibo.Id && x.RewardId == reward.Id && x.Pool == interaction.Pool && x.EntryIndex == interaction.SourceOrder);
            if (relation is null) { relation = new AmiiboReward { AmiiboId = amiibo.Id, RewardId = reward.Id, Pool = interaction.Pool, Probability = interaction.NormalizedProbability ?? 0, Condition = interaction.Condition, DropSourcePath = interaction.SourcePath, DropSourceSha256 = interaction.SourceHash, EntryIndex = interaction.SourceOrder }; db.AmiiboRewards.Add(relation); }
            relation.RawWeight = interaction.RawWeight; relation.MinCount = interaction.MinCount; relation.MaxCount = interaction.MaxCount; relation.SpecialMetadata = interaction.SpecialMetadata; relation.InteractionKind = interaction.InteractionKind; relation.OutcomeKind = interaction.OutcomeKind; relation.Probability = interaction.NormalizedProbability ?? 0; relation.Condition = interaction.Condition; count++;
        }
        var hit = (await TotkRomFsReader.ReadAsync(index.RootPath)).Settings.HitRates;
        var hitSummary = System.Text.Json.JsonSerializer.Serialize(hit);
        var hitRun = await db.ImportRuns.SingleOrDefaultAsync(x => x.GameId == game.Id && x.Kind == "totk-hit-rate-v1" && x.SourceSha256 == hash);
        if (hitRun is null) db.ImportRuns.Add(new ImportRun { GameId = game.Id, Kind = "totk-hit-rate-v1", SourcePath = $"{TotkRomFsReader.ResidentArchive}::{TotkRomFsReader.SettingsPath}", SourceSha256 = hash, Status = ImportStatus.Succeeded, FinishedAt = DateTimeOffset.UtcNow, Summary = hitSummary });
        else { hitRun.Status = ImportStatus.Succeeded; hitRun.FinishedAt = DateTimeOffset.UtcNow; hitRun.Summary = hitSummary; }
        var messages = await TotkMessageCatalogReader.ReadAsync(index.RootPath, locale);
        var localized = 0;
        foreach (var reward in await db.Rewards.Where(x => x.GameId == game.Id).ToListAsync())
        {
            var name = ResolveTotkMessage(messages, reward.InternalId, "Name") ?? ResolveTotkMessage(messages, FishIconAliases.Resolve(reward.InternalId), "Name"); var description = ResolveTotkMessage(messages, reward.InternalId, "Caption") ?? ResolveTotkMessage(messages, reward.InternalId, "Desc") ?? ResolveTotkMessage(messages, FishIconAliases.Resolve(reward.InternalId), "Caption") ?? ResolveTotkMessage(messages, FishIconAliases.Resolve(reward.InternalId), "Desc");
            if (name is null && description is null) continue;
            var item = await db.RewardLocalizations.SingleOrDefaultAsync(x => x.RewardId == reward.Id && x.Locale == locale);
            if (item is null) db.RewardLocalizations.Add(new RewardLocalization { RewardId = reward.Id, Locale = locale, Name = name ?? reward.Name, Description = description ?? reward.Description, SourcePath = $"Mals/{locale}.Product.*.sarc.zs", SourceSha256 = hash });
            else { if (name is not null) item.Name = name; if (description is not null) item.Description = description; item.SourcePath = $"Mals/{locale}.Product.*.sarc.zs"; item.SourceSha256 = hash; }
            localized++;
        }
        var unresolvedNames = interactions.Select(x => x.SourceId).Distinct().Count(sourceId => ResolveTotkMessage(messages, sourceId, "Name") is null);
        run.Status = unresolvedNames == 0 ? ImportStatus.Succeeded : ImportStatus.SucceededWithWarnings; run.FinishedAt = DateTimeOffset.UtcNow; run.Summary = $"{interactions.Count} normalized interactions; {interactions.Select(x => x.SelectorId).Distinct().Count()} selectors; {localized} localized outcomes; {unresolvedNames} unresolved names.";
        await db.SaveChangesAsync(); log.LogInformation("{Summary}", run.Summary); return 0;
    }
    catch (Exception ex) { run.Status = ImportStatus.Failed; run.FinishedAt = DateTimeOffset.UtcNow; run.Summary = ex.Message; await db.SaveChangesAsync(); log.LogError(ex, "TOTK import failed"); return 1; }
}
static string? ResolveTotkMessage(IReadOnlyDictionary<string, string> messages, string sourceId, string suffix)
{
    var exact = messages.GetValueOrDefault($"{sourceId}_{suffix}") ?? messages.GetValueOrDefault($"{sourceId}.{suffix}");
    if (exact is not null) return exact;
    return messages.FirstOrDefault(x => x.Key.Equals(sourceId, StringComparison.Ordinal) || x.Key.Equals($"{sourceId}{suffix}", StringComparison.OrdinalIgnoreCase) || x.Key.Equals($"{sourceId}_{suffix}", StringComparison.OrdinalIgnoreCase)).Value;
}
static RewardType ToRewardType(OutcomeKind kind) => kind switch { OutcomeKind.ActorSpawn => RewardType.ActorSpawn, OutcomeKind.Container => RewardType.Container, OutcomeKind.Companion => RewardType.Companion, OutcomeKind.Special => RewardType.Special, OutcomeKind.CostumeUnlock => RewardType.CostumeUnlock, OutcomeKind.CharacterUnlock => RewardType.CharacterUnlock, OutcomeKind.ModeUnlock => RewardType.ModeUnlock, OutcomeKind.Event => RewardType.Event, _ => RewardType.Item };
static string SelectorDisplayName(string selector) => selector == "Default" ? "Amiibo no-Zelda (regla predeterminada)" : selector.Replace("NumberingID_", "Amiibo #", StringComparison.Ordinal).Replace("CharacterID_", "Amiibo ", StringComparison.Ordinal).Replace("CharacterBaseID_", "Familia ", StringComparison.Ordinal);
static async Task<int> ImportAmiiboBinsAsync(string root, AmiiboRewardsDbContext db, ILogger log)
{
    var result = await AmiiboCatalogImporter.ScanAsync(root);
    await db.Database.MigrateAsync();
    foreach (var entry in result.Entries)
    {
        var stored = await db.AmiiboCatalog.SingleOrDefaultAsync(x => x.SourceFile == entry.SourceFile);
        if (stored is null) db.AmiiboCatalog.Add(entry);
        else { stored.DisplayName = entry.DisplayName; stored.CharacterId = entry.CharacterId; stored.CharacterBaseId = entry.CharacterBaseId; stored.SeriesId = entry.SeriesId; stored.NumberingId = entry.NumberingId; stored.NfpType = entry.NfpType; stored.Version = entry.Version; stored.SourceSha256 = entry.SourceSha256; stored.ImportedAt = DateTimeOffset.UtcNow; }
    }
    await db.SaveChangesAsync();
    var catalog = AmiiboAssociationExpansion.Deduplicate(await db.AmiiboCatalog.AsNoTracking().ToListAsync());
    var totk = await db.Games.SingleOrDefaultAsync(x => x.Code == "TOTK"); var enriched = 0;
    if (totk is not null)
    {
        foreach (var amiibo in await db.Amiibo.Where(x => x.GameId == totk.Id).ToListAsync())
        {
            var (kind, value) = ParseSelector(amiibo.InternalTableId);
            var matches = kind is null ? [] : catalog.Where(x => TotkAmiiboRuleMatcher.MatchesSelector(kind.Value, value, x)).OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
            if (matches.Count == 0) continue;
            // A single-match selector already names one concrete amiibo; multi-match selectors keep the
            // generic family label here and are expanded into separate rows by the API at read time.
            if (matches.Count == 1) amiibo.Name = matches[0].DisplayName;
            amiibo.MappingStatus = AmiiboMappingStatus.Confirmed; amiibo.MappingSource = "Local amiibo catalog"; enriched++;
        }
        await db.SaveChangesAsync();
    }
    log.LogInformation("Amiibo catalog: {Scanned} scanned, {Valid} valid, {Invalid} invalid, {Enriched} TOTK selectors enriched.", result.ScannedFiles, result.Entries.Count, result.InvalidFiles, enriched);
    return 0;
}
static async Task<int> DiagnoseTotkSelectorsAsync(string root, AmiiboRewardsDbContext db)
{
    var result = await AmiiboCatalogImporter.ScanAsync(root); var ids = new ushort[] { 1048, 1050, 1049, 851, 852, 854, 843, 844, 846, 847, 4, 845, 848, 892, 921, 850, 14, 1044 };
    foreach (var id in ids)
    {
        var matches = result.Entries.Where(x => x.NumberingId == id).ToList(); Console.WriteLine($"NumberingID {id}: {matches.Count} matches");
        foreach (var match in matches) Console.WriteLine($"  {match.DisplayName} | {match.CharacterId} | {match.SourceFile}");
    }
    Console.WriteLine($"Scanned: {result.ScannedFiles}; valid: {result.Entries.Count}; invalid: {result.InvalidFiles}"); return 0;
}
static (TotkSelectorKind? Kind, string Value) ParseSelector(string selector)
{
    var (kind, value) = AmiiboAssociationExpansion.ParseSelector(selector);
    return kind == TotkSelectorKind.Default ? (null, "") : (kind, value);
}
static int AnalyzeRomFs(string path)
{
    var index = RomFsIndex.Create(path); var report = RomFsAnalyzer.Analyze(index);
    var detectedProfile = report.Game?.Code is { } code ? GameProfiles.Find(code) : null;
    Console.WriteLine($"RomFS: {index.RootPath}\nGame: {report.Game?.Name ?? "unknown"}\nProfile: {report.Game?.Code ?? "unknown"}\nTitleId: {report.Game?.Code switch { "BOTW" => GameProfiles.Botw.TitleId, "TOTK" => GameProfiles.Totk.TitleId, _ => "unknown" }}\nFiles: {report.FileCount}");
    foreach (var source in detectedProfile?.SemanticSourcePaths ?? []) Console.WriteLine($"semantic source: {source}");
    foreach (var format in report.FormatCounts.Where(x => x.Key != SwitchDataFormat.Unknown)) Console.WriteLine($"format {format.Key}: {format.Value}");
    foreach (var keyword in report.KeywordMatches) Console.WriteLine($"keyword {keyword.Key}: {keyword.Value.Count} files");
    PrintCandidates("BYML", report.BymlCandidates); PrintCandidates("AAMP", report.AampCandidates); PrintCandidates("MSBT", report.MsbtCandidates);
    PrintCandidates("localization", report.LocalizationCandidates); PrintCandidates("texture", report.TextureCandidates); PrintCandidates("SARC", report.SarcCandidates);
    return 0;
}
static void PrintCandidates(string label, IReadOnlyList<RomFsFile> files) { Console.WriteLine($"{label} candidates: {files.Count}"); foreach (var file in files.Take(20)) Console.WriteLine($"  {file.RelativePath}"); }
static async Task<int> ImportLocalizationAsync(string locale, AmiiboRewardsDbContext db, ILogger log)
{
    if (!System.Text.RegularExpressions.Regex.IsMatch(locale, "^[A-Za-z]{4}$")) { Console.Error.WriteLine("Locale must be a four-character BOTW locale, for example USes."); return 2; }
    var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Botw__RomFsPath");
    if (string.IsNullOrWhiteSpace(romFs)) { Console.Error.WriteLine("Set AMIIBOREWARDS_Botw__RomFsPath to the RomFS directory."); return 2; }
    var packPath = Path.Combine(romFs, "Pack", $"Bootup_{locale}.pack");
    if (!File.Exists(packPath)) { Console.Error.WriteLine($"Localization pack not found: {packPath}"); return 2; }

    var packHash = Convert.ToHexString(await SHA256.HashDataAsync(File.OpenRead(packPath))).ToLowerInvariant();
    var bootup = SwitchArchive.DecodeCompression(await File.ReadAllBytesAsync(packPath));
    var messageArchiveName = SwitchArchive.ReadNames(bootup).FirstOrDefault(x => x.EndsWith(".product.ssarc", StringComparison.OrdinalIgnoreCase) && x.Contains(locale, StringComparison.OrdinalIgnoreCase));
    if (messageArchiveName is null) { Console.Error.WriteLine($"No message archive for {locale} was found in {packPath}."); return 2; }
    var messages = SwitchArchive.DecodeCompression(SwitchArchive.Extract(bootup, messageArchiveName));
    var allText = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var entry in SwitchArchive.ReadMessageCatalog(messages)) allText[entry.Key] = entry.Value;

    await db.Database.MigrateAsync();
    var game = await db.Games.SingleOrDefaultAsync(x => x.Code == "BOTW");
    if (game is null) { Console.Error.WriteLine("Import rewards before importing localization."); return 2; }
    // Increment when the official label-alias table gains coverage so existing packs are re-evaluated.
    var kind = $"localization-{locale}-v2";
    var run = await db.ImportRuns.SingleOrDefaultAsync(x => x.GameId == game.Id && x.Kind == kind && x.SourceSha256 == packHash);
    if ((run?.Status is ImportStatus.Succeeded or ImportStatus.SucceededWithWarnings) && await db.RewardLocalizations.AnyAsync(x => x.Locale == locale)) { log.LogInformation("Localization source already imported: {Hash}", packHash); return 0; }
    if (run is null) { run = new ImportRun { GameId = game.Id, Kind = kind, SourcePath = packPath, SourceSha256 = packHash, Status = ImportStatus.Running }; db.ImportRuns.Add(run); }
    else { run.Status = ImportStatus.Running; run.Summary = null; run.FinishedAt = null; }

    try
    {
        var updated = 0; var unresolved = 0;
        var rewards = await db.Rewards.Where(r => r.GameId == game.Id).ToListAsync();
        foreach (var reward in rewards)
        {
            var candidates = LocalizationIds(reward.InternalId);
            var localizedName = candidates.Select(id => allText.GetValueOrDefault($"{id}_Name")).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            var localizedDescription = candidates.Select(id => allText.GetValueOrDefault($"{id}_Desc")).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (localizedName is null && localizedDescription is null) { unresolved++; continue; }
            var localization = await db.RewardLocalizations.SingleOrDefaultAsync(x => x.RewardId == reward.Id && x.Locale == locale);
            if (localization is null)
            {
                localization = new RewardLocalization { RewardId = reward.Id, Locale = locale, Name = localizedName ?? reward.Name, Description = localizedDescription ?? reward.Description, SourcePath = $"Pack/Bootup_{locale}.pack::{messageArchiveName}", SourceSha256 = packHash };
                db.RewardLocalizations.Add(localization);
            }
            else
            {
                if (localizedName is not null) localization.Name = localizedName;
                if (localizedDescription is not null) localization.Description = localizedDescription;
                localization.SourcePath = $"Pack/Bootup_{locale}.pack::{messageArchiveName}";
                localization.SourceSha256 = packHash;
            }
            updated++;
        }
        run.Status = unresolved == 0 ? ImportStatus.Succeeded : ImportStatus.SucceededWithWarnings;
        run.Summary = $"{locale}: {updated} localized rewards; {unresolved} without a matching MSBT label.";
        run.FinishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        log.LogInformation("{Summary}", run.Summary);
        return 0;
    }
    catch (Exception ex) { run.Status = ImportStatus.Failed; run.FinishedAt = DateTimeOffset.UtcNow; run.Summary = ex.Message; await db.SaveChangesAsync(); log.LogError(ex, "Localization import failed"); return 1; }
}
static async Task<int> InspectLocalizationAsync(string locale, string label)
{
    var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Botw__RomFsPath");
    var packPath = string.IsNullOrWhiteSpace(romFs) ? null : Path.Combine(romFs, "Pack", $"Bootup_{locale}.pack");
    if (packPath is null || !File.Exists(packPath)) { Console.Error.WriteLine("Localization pack not found. Set AMIIBOREWARDS_Botw__RomFsPath."); return 2; }
    var bootup = SwitchArchive.DecodeCompression(await File.ReadAllBytesAsync(packPath));
    var archive = SwitchArchive.ReadNames(bootup).FirstOrDefault(x => x.EndsWith(".product.ssarc", StringComparison.OrdinalIgnoreCase) && x.Contains(locale, StringComparison.OrdinalIgnoreCase));
    if (archive is null) { Console.Error.WriteLine("Message archive not found."); return 2; }
    var messages = SwitchArchive.DecodeCompression(SwitchArchive.Extract(bootup, archive));
    var value = SwitchArchive.ReadMessageCatalog(messages).GetValueOrDefault(label);
    if (value is not null) { Console.WriteLine(value); return 0; }
    Console.Error.WriteLine($"Label not found: {label}"); return 1;
}
static async Task<int> FindLocalizationAsync(string locale, string fragment)
{
    var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Botw__RomFsPath");
    var packPath = string.IsNullOrWhiteSpace(romFs) ? null : Path.Combine(romFs, "Pack", $"Bootup_{locale}.pack");
    if (packPath is null || !File.Exists(packPath)) { Console.Error.WriteLine("Localization pack not found. Set AMIIBOREWARDS_Botw__RomFsPath."); return 2; }
    var bootup = SwitchArchive.DecodeCompression(await File.ReadAllBytesAsync(packPath));
    var archive = SwitchArchive.ReadNames(bootup).FirstOrDefault(x => x.EndsWith(".product.ssarc", StringComparison.OrdinalIgnoreCase) && x.Contains(locale, StringComparison.OrdinalIgnoreCase));
    if (archive is null) { Console.Error.WriteLine("Message archive not found."); return 2; }
    var messages = SwitchArchive.DecodeCompression(SwitchArchive.Extract(bootup, archive));
    var matches = 0;
    foreach (var entry in SwitchArchive.ReadMessageCatalog(messages).Where(x => x.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase))) { Console.WriteLine($"{entry.Key} = {entry.Value}"); matches++; }
    return matches == 0 ? 1 : 0;
}
static IEnumerable<string> LocalizationIds(string id)
{
    yield return id;
    if (id.StartsWith("Animal_Fish_", StringComparison.Ordinal)) yield return id.Replace("Animal_Fish_", "Item_FishGet_", StringComparison.Ordinal);
    // These AAMP actor IDs map to the canonical inventory-message keys in MSBT.
    if (id == "Obj_ArrowNormal_A_01") yield return "NormalArrow";
    if (id == "Obj_AncientArrow_A_01") yield return "AncientArrow";
}
static RewardType Classify(string id) => id is "Barrel" or "BarrelBomb" or "Kibako_Contain_01" or "Obj_BreakBoxIron" ? RewardType.Container : id.Contains("DropTable", StringComparison.OrdinalIgnoreCase) ? RewardType.SecondaryDropTable : id.StartsWith("Obj_", StringComparison.Ordinal) ? RewardType.ActorSpawn : RewardType.Item;
static string? Condition(string pool) => pool.Contains('(') ? pool[(pool.IndexOf('(') + 1)..].TrimEnd(')') : null;
static async Task<string> SourceHashAsync(string sourcePack, string fallback) { var romFs = Environment.GetEnvironmentVariable("AMIIBOREWARDS_Botw__RomFsPath"); var path = string.IsNullOrWhiteSpace(romFs) ? null : Path.Combine(romFs, "Actor", "Pack", sourcePack); return path is not null && File.Exists(path) ? Convert.ToHexString(await SHA256.HashDataAsync(File.OpenRead(path))).ToLowerInvariant() : fallback; }
sealed record DropTable(
    [property: JsonPropertyName("table_id")] string TableId,
    [property: JsonPropertyName("source_pack")] string SourcePack,
    [property: JsonPropertyName("source_bdrop")] string SourceBdrop,
    [property: JsonPropertyName("pools")] List<DropPool> Pools);
sealed record DropPool(string Name, List<DropEntry> Rewards);
sealed record DropEntry(
    [property: JsonPropertyName("item_id")] string ItemId,
    [property: JsonPropertyName("probability")] decimal Probability,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("category_source")] string? CategorySource);
sealed record AmiiboCatalogEntry(string TableId, string? Name, AmiiboMappingStatus MappingStatus = AmiiboMappingStatus.Confirmed, string? MappingSource = null);
