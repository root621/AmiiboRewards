using System.Diagnostics;
using System.Security.Cryptography;
using AmiiboRewards.Domain;
using AmiiboRewards.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true).AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"), optional: true).AddEnvironmentVariables(prefix: "AMIIBOREWARDS_").Build();
using var factory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true)); var log = factory.CreateLogger("Assets");
var romFs = config["Botw:RomFsPath"]; var extractor = config["Botw:BntxExtractorPath"]; var magick = config["Botw:ImageMagickExecutable"] ?? "magick"; var root = Path.GetFullPath(config["Assets:RootPath"] ?? "assets");
var totkRomFs = config["Totk:RomFsPath"]; var totkExtractor = config["Totk:BntxExtractorPath"]; var totkMagick = config["Totk:ImageMagickExecutable"] ?? magick; var totkAstcEncoder = config["Totk:AstcEncoderPath"] ?? "astcenc";
if (args.Length == 0 || args[0] is "--help" or "-h") { Console.WriteLine("Commands: extract-icon <item-id>, sync-icons, sync-totk-icons"); return 0; }
if (args[0] == "extract-icon" && args.Length == 2) { if (string.IsNullOrWhiteSpace(romFs) || string.IsNullOrWhiteSpace(extractor)) { log.LogError("Configure Botw:RomFsPath and Botw:BntxExtractorPath."); return 2; } await ExtractAsync(args[1], romFs, extractor, magick, root, log, CancellationToken.None); return 0; }
if (args[0] == "sync-totk-icons") { return await SyncTotkIconsAsync(totkRomFs, totkExtractor, totkMagick, totkAstcEncoder, root, config.GetConnectionString("AmiiboRewards"), log, CancellationToken.None); }
if (args[0] != "sync-icons") { Console.Error.WriteLine("Usage: extract-icon <item-id> | sync-icons"); return 2; }
if (string.IsNullOrWhiteSpace(romFs) || string.IsNullOrWhiteSpace(extractor)) { log.LogError("Configure Botw:RomFsPath and Botw:BntxExtractorPath."); return 2; }
var connection = string.IsNullOrWhiteSpace(config.GetConnectionString("AmiiboRewards")) ? "Host=localhost;Port=5432;Database=amiibo_rewards;Username=amiibo;Password=amiibo" : config.GetConnectionString("AmiiboRewards"); var options = new DbContextOptionsBuilder<AmiiboRewardsDbContext>().UseNpgsql(connection).Options; await using var db = new AmiiboRewardsDbContext(options); var game = await db.Games.SingleAsync(g => g.Code == "BOTW"); var rewards = await db.Rewards.Where(r => r.GameId == game.Id).ToListAsync(); var made = 0; var skipped = 0; var errors = 0;
foreach (var reward in rewards) { try { var iconId = FishIconAliases.Resolve(reward.InternalId); var source = Path.Combine(romFs, "UI", "StockItem", $"{iconId}.sbitemico"); if (!File.Exists(source)) { log.LogWarning("No direct icon for {Id}", reward.InternalId); errors++; continue; } var hash = Convert.ToHexString(await SHA256.HashDataAsync(File.OpenRead(source))).ToLowerInvariant(); var relative = Path.Combine("botw", "icons", $"{reward.InternalId}.png"); var output = Path.Combine(root, relative); var asset = await db.Assets.SingleOrDefaultAsync(a => a.GameId == game.Id && a.InternalId == reward.InternalId && a.AssetType == AssetType.Icon); if (asset?.SourceSha256 == hash && File.Exists(output) && File.Exists(output + ".rgba-v2")) { skipped++; continue; } await ExtractAsync(reward.InternalId, romFs, extractor, magick, root, log, CancellationToken.None); var isNew = asset is null; asset ??= new Asset { GameId = game.Id, InternalId = reward.InternalId, AssetType = AssetType.Icon, SourcePath = Path.Combine("UI", "StockItem", $"{iconId}.sbitemico"), OutputPath = relative, SourceSha256 = hash }; asset.SourcePath = Path.Combine("UI", "StockItem", $"{iconId}.sbitemico"); asset.SourceSha256 = hash; asset.UpdatedAt = DateTimeOffset.UtcNow; if (isNew) db.Assets.Add(asset); reward.IconAsset = asset; made++; } catch (Exception ex) { log.LogError(ex, "Failed {Id}", reward.InternalId); errors++; } }
await db.SaveChangesAsync(); log.LogInformation("sync-icons: {Made} generated, {Skipped} unchanged, {Errors} unresolved/errors", made, skipped, errors); return errors == 0 ? 0 : 1;

static async Task<string> ExtractAsync(string requestedId, string romFs, string extractor, string magick, string root, ILogger log, CancellationToken ct)
{ var iconId = FishIconAliases.Resolve(requestedId); var source = Path.Combine(romFs, "UI", "StockItem", $"{iconId}.sbitemico"); if (!File.Exists(source)) throw new FileNotFoundException("Icon source was not found.", source); var temp = Path.Combine(Path.GetTempPath(), "amiibo-rewards", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp); try { var decoded = Yaz0Decoder.Decode(await File.ReadAllBytesAsync(source, ct)); const int offset = 4096; if (decoded.Length < offset + 4 || !decoded.AsSpan(offset, 4).SequenceEqual("BNTX"u8)) throw new InvalidDataException("Expected BNTX at offset 4096."); var bntx = Path.Combine(temp, $"{iconId}.bntx"); await File.WriteAllBytesAsync(bntx, decoded[offset..], ct); await RunAsync("python3", $"\"{extractor}\" \"{bntx}\"", temp, ct); var dds = Directory.EnumerateFiles(temp, "*.dds", SearchOption.AllDirectories).SingleOrDefault() ?? throw new FileNotFoundException("BNTX-Extractor produced no DDS file."); var output = Path.Combine(root, "botw", "icons", $"{requestedId}.png"); Directory.CreateDirectory(Path.GetDirectoryName(output)!); var rgba = DdsRgbaDecoder.TryDecode(await File.ReadAllBytesAsync(dds, ct));
if (rgba is not null)
{
    var raw = Path.Combine(temp, "pixels.rgba");
    await File.WriteAllBytesAsync(raw, rgba.Pixels, ct);
    await RunAsync(magick, $"-size {rgba.Width}x{rgba.Height} -depth 8 \"rgba:{raw}\" \"{output}\"", temp, ct);
}
else await RunAsync(magick, $"\"{dds}\" \"{output}\"", temp, ct); if (!File.Exists(output) || new FileInfo(output).Length == 0) throw new InvalidDataException("ImageMagick did not produce a PNG."); await File.WriteAllTextAsync(output + ".rgba-v2", "DDS channel masks respected; RGBA conversion v2.", ct); log.LogInformation("Created {Output}", output); return output; } finally { Directory.Delete(temp, true); } }
static async Task RunAsync(string executable, string arguments, string cwd, CancellationToken ct) { using var p = Process.Start(new ProcessStartInfo(executable, arguments) { WorkingDirectory = cwd, RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false }) ?? throw new InvalidOperationException($"Could not start {executable}."); var output = await p.StandardOutput.ReadToEndAsync(ct); var error = await p.StandardError.ReadToEndAsync(ct); await p.WaitForExitAsync(ct); if (p.ExitCode != 0) throw new InvalidOperationException($"{executable} failed ({p.ExitCode}): {error}\n{output}"); }

static async Task<int> SyncTotkIconsAsync(string? romFs, string? extractor, string magick, string astcEncoder, string root, string? configuredConnection, ILogger log, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(romFs) || string.IsNullOrWhiteSpace(extractor)) { log.LogError("Configure Totk:RomFsPath and Totk:BntxExtractorPath."); return 2; }
    var index = RomFsIndex.Create(romFs); var resolver = new TotkAssetResolver(); var options = new DbContextOptionsBuilder<AmiiboRewardsDbContext>().UseNpgsql(configuredConnection ?? "Host=localhost;Port=5432;Database=amiibo_rewards;Username=amiibo;Password=amiibo").Options;
    await using var db = new AmiiboRewardsDbContext(options); var game = await db.Games.SingleAsync(g => g.Code == "TOTK", ct); var rewards = await db.Rewards.Where(r => r.GameId == game.Id).ToListAsync(ct); var made = 0; var skipped = 0; var missing = 0;
    foreach (var reward in rewards)
    {
        var source = await resolver.ResolveAsync(GameProfiles.Totk, index, reward.InternalId, ct); if (source is null) { missing++; continue; }
        var sourcePath = index.FullPath(index.Files.Single(x => x.RelativePath.Equals(source, StringComparison.OrdinalIgnoreCase))); var hash = Convert.ToHexString(await SHA256.HashDataAsync(File.OpenRead(sourcePath), ct)).ToLowerInvariant(); var relative = Path.Combine("totk", "icons", $"{reward.InternalId}.png"); var output = Path.Combine(root, relative); var asset = await db.Assets.SingleOrDefaultAsync(a => a.GameId == game.Id && a.InternalId == reward.InternalId && a.AssetType == AssetType.Icon, ct);
        if (asset?.SourceSha256 == hash && File.Exists(output)) { skipped++; continue; }
        try { await ExtractTotkAsync(reward.InternalId, sourcePath, extractor, magick, astcEncoder, output, ct); var isNew = asset is null; asset ??= new Asset { GameId = game.Id, InternalId = reward.InternalId, AssetType = AssetType.Icon, SourcePath = source, OutputPath = relative, SourceSha256 = hash }; asset.SourcePath = source; asset.OutputPath = relative; asset.SourceSha256 = hash; asset.UpdatedAt = DateTimeOffset.UtcNow; if (isNew) db.Assets.Add(asset); reward.IconAsset = asset; made++; }
        catch (Exception ex) { log.LogWarning(ex, "Could not extract TOTK icon {Id}", reward.InternalId); }
    }
    await db.SaveChangesAsync(ct); log.LogInformation("TOTK icons: {Made} generated, {Skipped} unchanged, {Missing} native sources missing.", made, skipped, missing); return 0;
}

static async Task ExtractTotkAsync(string requestedId, string source, string extractor, string magick, string astcEncoder, string output, CancellationToken ct)
{
    var temp = Path.Combine(Path.GetTempPath(), "amiibo-rewards", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
    try { var bntx = Path.Combine(temp, $"{requestedId}.bntx"); if (source.EndsWith(".zs", StringComparison.OrdinalIgnoreCase)) await ZstdReader.DecompressAsync(source, bntx, cancellationToken: ct); else File.Copy(source, bntx); await RunAsync("python3", $"\"{extractor}\" \"{bntx}\"", temp, ct); Directory.CreateDirectory(Path.GetDirectoryName(output)!); var dds = Directory.EnumerateFiles(temp, "*.dds", SearchOption.AllDirectories).SingleOrDefault(); var astc = Directory.EnumerateFiles(temp, "*.astc", SearchOption.AllDirectories).SingleOrDefault(); if (dds is not null) await RunAsync(magick, $"\"{dds}\" \"{output}\"", temp, ct); else if (astc is not null) await RunAsync(astcEncoder, $"-dl \"{astc}\" \"{output}\"", temp, ct); else throw new FileNotFoundException("BNTX extractor produced no DDS or ASTC texture."); if (!File.Exists(output) || new FileInfo(output).Length == 0) throw new InvalidDataException("Icon decoder produced no PNG."); }
    finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
}
