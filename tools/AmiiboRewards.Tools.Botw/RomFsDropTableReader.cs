using System.Text.RegularExpressions;
using System.Security.Cryptography;
using AmiiboRewards.Domain;

namespace AmiiboRewards.Tools.Botw;

/// <summary>Discovers BOTW amiibo drop-table packs and extracts their BDrop payloads from RomFS.</summary>
internal static partial class RomFsDropTableReader
{
    public static async Task<IReadOnlyList<RomFsDropTable>> ReadAsync(string romFsPath, CancellationToken cancellationToken = default)
    {
        var packsPath = Path.Combine(romFsPath, "Actor", "Pack");
        if (!Directory.Exists(packsPath)) throw new DirectoryNotFoundException($"RomFS Actor/Pack directory not found: {packsPath}");

        var tables = new List<RomFsDropTable>();
        foreach (var packPath in Directory.EnumerateFiles(packsPath, "Item_Amiibo_DropTable_*.sbactorpack").OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var match = PackName().Match(Path.GetFileName(packPath));
            if (!match.Success) continue;
            var pack = await File.ReadAllBytesAsync(packPath, cancellationToken);
            var archive = SwitchArchive.DecodeCompression(pack);
            var entries = SwitchArchive.ReadNames(archive).Where(x => x.EndsWith(".bdrop", StringComparison.OrdinalIgnoreCase)).ToList();
            if (entries.Count != 1) throw new InvalidDataException($"Expected one .bdrop in {Path.GetFileName(packPath)}, found {entries.Count}.");
            var bdropName = entries[0];
            var bdrop = SwitchArchive.Extract(archive, bdropName);
            if (!bdrop.AsSpan().StartsWith("AAMP"u8)) throw new InvalidDataException($"{bdropName} in {Path.GetFileName(packPath)} is not AAMP.");
            tables.Add(new RomFsDropTable(match.Groups["id"].Value, Path.GetFileName(packPath), bdropName, bdrop, Convert.ToHexString(SHA256.HashData(pack)).ToLowerInvariant()));
        }
        if (tables.Count == 0) throw new InvalidDataException($"No amiibo drop-table packs were found in {packsPath}.");
        return tables;
    }

    [GeneratedRegex("^Item_Amiibo_DropTable_(?<id>\\d{3})\\.sbactorpack$", RegexOptions.CultureInvariant)]
    private static partial Regex PackName();
}

internal sealed record RomFsDropTable(string TableId, string SourcePack, string SourceBdrop, byte[] Bdrop, string SourceSha256);
