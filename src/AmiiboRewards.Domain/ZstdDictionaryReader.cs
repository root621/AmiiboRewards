namespace AmiiboRewards.Domain;

/// <summary>Loads one named Zstandard dictionary from a compressed SARC dictionary bundle.</summary>
public static class ZstdDictionaryReader
{
    public static async Task<byte[]> ReadAsync(string dictionaryBundlePath, string entryName, CancellationToken cancellationToken = default)
    {
        var temp = Path.GetTempFileName(); File.Delete(temp);
        try
        {
            await ZstdReader.DecompressAsync(dictionaryBundlePath, temp, cancellationToken: cancellationToken);
            return SarcReader.Extract(await File.ReadAllBytesAsync(temp, cancellationToken), entryName);
        }
        finally { File.Delete(temp); }
    }
}
