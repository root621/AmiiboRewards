using System.Security.Cryptography;

namespace AmiiboRewards.Domain;

public sealed record AmiiboCatalogImportResult(
    IReadOnlyList<AmiiboCatalogEntry> Entries,
    int ScannedFiles,
    int InvalidFiles);

public static class AmiiboCatalogImporter
{
    public static async Task<AmiiboCatalogImportResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException($"Amiibo collection not found: {rootPath}");
        var entries = new List<AmiiboCatalogEntry>(); var scanned = 0; var invalid = 0;
        foreach (var path in Directory.EnumerateFiles(rootPath, "*.bin", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested(); scanned++;
            try
            {
                var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
                var identity = AmiiboIdentity.Read(bytes);
                entries.Add(new AmiiboCatalogEntry
                {
                    SourceFile = Path.GetFullPath(path), DisplayName = DisplayName(path),
                    CharacterId = Format(identity.CharacterId), CharacterBaseId = Format(identity.CharacterBaseId),
                    SeriesId = identity.SeriesId, NumberingId = identity.NumberingId, NfpType = identity.NfpType,
                    Version = identity.Version, SourceSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
                });
            }
            catch (InvalidDataException) { invalid++; }
            catch (IOException) { invalid++; }
            catch (UnauthorizedAccessException) { invalid++; }
        }
        return new(entries, scanned, invalid);
    }

    public static string DisplayName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path).Trim();
        var separator = name.IndexOf(" - ", StringComparison.Ordinal);
        if (separator > 0 && name[..separator].All(char.IsDigit)) name = name[(separator + 3)..].Trim();
        return string.IsNullOrWhiteSpace(name) ? Path.GetFileName(path) : name;
    }

    public static string Format(ReadOnlySpan<byte> value) => string.Join('-', value.ToArray().Select(x => x.ToString("X2")));
}