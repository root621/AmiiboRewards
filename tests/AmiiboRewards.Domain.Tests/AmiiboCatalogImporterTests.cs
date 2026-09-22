using System.Buffers.Binary;
using AmiiboRewards.Domain;

namespace AmiiboRewards.Domain.Tests;

public sealed class AmiiboCatalogImporterTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "amiibo-catalog-" + Guid.NewGuid());

    [Fact]
    public async Task Scans_valid_bin_and_ignores_invalid_file()
    {
        Directory.CreateDirectory(Path.Combine(root, "Zelda"));
        var bytes = new byte[0x5c]; bytes[0x54] = 1; bytes[0x55] = 0; bytes[0x56] = 1; bytes[0x57] = 0; BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(0x58), 1048); bytes[0x5a] = 9; bytes[0x5b] = 2;
        await File.WriteAllBytesAsync(Path.Combine(root, "Zelda", "Link (TotK).bin"), bytes);
        await File.WriteAllBytesAsync(Path.Combine(root, "bad.bin"), [1, 2, 3]);

        var result = await AmiiboCatalogImporter.ScanAsync(root);

        var entry = Assert.Single(result.Entries); Assert.Equal(2, result.ScannedFiles); Assert.Equal(1, result.InvalidFiles);
        Assert.Equal("Link (TotK)", entry.DisplayName); Assert.Equal((ushort)1048, entry.NumberingId); Assert.Equal("01-00-01", entry.CharacterId); Assert.Equal("01-00", entry.CharacterBaseId);
    }

    [Fact]
    public void Selector_matching_uses_identity_fields()
    {
        var entry = new AmiiboCatalogEntry { SourceFile = "link.bin", DisplayName = "Link (TotK)", CharacterId = "01-00-01", CharacterBaseId = "01-00", NumberingId = 1048, SourceSha256 = "hash" };
        Assert.True(TotkAmiiboRuleMatcher.MatchesSelector(TotkSelectorKind.NumberingId, "1048", entry));
        Assert.True(TotkAmiiboRuleMatcher.MatchesSelector(TotkSelectorKind.CharacterId, "ToonLink", entry));
        Assert.True(TotkAmiiboRuleMatcher.MatchesSelector(TotkSelectorKind.CharacterBaseId, "Link", entry));
        Assert.False(TotkAmiiboRuleMatcher.MatchesSelector(TotkSelectorKind.NumberingId, "848", entry));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}