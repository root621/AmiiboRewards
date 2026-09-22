using AmiiboRewards.Domain;

namespace AmiiboRewards.Tools.Botw;

/// <summary>TOTK only supplies archive hints; Zstd/SARC/MSBT parsing is shared infrastructure.</summary>
internal static class TotkMessageCatalogReader
{
    public static async Task<IReadOnlyDictionary<string, string>> ReadAsync(string romFsPath, string locale, CancellationToken ct = default)
    {
        var mals = Path.Combine(romFsPath, "Mals");
        var archive = Directory.Exists(mals) ? Directory.EnumerateFiles(mals, $"{locale}.Product.*.sarc.zs").OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault() : null;
        if (archive is null) return new Dictionary<string, string>(StringComparer.Ordinal);
        var zsdic = Path.Combine(romFsPath, "Pack", "ZsDic.pack.zs"); if (!File.Exists(zsdic)) throw new FileNotFoundException("TOTK Zstd dictionary bundle was not found.");
        var temp = Directory.CreateTempSubdirectory("amiibo-totk-mals-");
        try
        {
            var dictionary = Path.Combine(temp.FullName, "zs.zsdic"); await File.WriteAllBytesAsync(dictionary, await ZstdDictionaryReader.ReadAsync(zsdic, "zs.zsdic", ct), ct);
            var unpacked = Path.Combine(temp.FullName, "messages.sarc"); await ZstdReader.DecompressAsync(archive, unpacked, dictionary, ct);
            var data = await File.ReadAllBytesAsync(unpacked, ct); var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in SwitchArchive.ReadMessageCatalog(data)) result.TryAdd(entry.Key, entry.Value);
            return result;
        }
        finally { temp.Delete(true); }
    }
}
