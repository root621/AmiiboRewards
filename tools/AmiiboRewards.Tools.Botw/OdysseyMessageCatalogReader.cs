using AmiiboRewards.Domain;

namespace AmiiboRewards.Tools.Botw;

internal sealed record OdysseyMessage(string ArchivePath, string MsbtPath, string Label, string Text);

internal static class OdysseyMessageCatalogReader
{
    public static async Task<IReadOnlyList<OdysseyMessage>> ReadAsync(string romFsPath, string locale, CancellationToken ct = default)
    {
        var messageRoot = Path.Combine(romFsPath, "LocalizedData", locale, "MessageData");
        if (!Directory.Exists(messageRoot)) return [];

        var messages = new List<OdysseyMessage>();
        foreach (var archivePath in Directory.EnumerateFiles(messageRoot, "*.szs").OrderBy(x => x, StringComparer.Ordinal))
        {
            var archive = SwitchArchive.DecodeCompression(await File.ReadAllBytesAsync(archivePath, ct));
            foreach (var msbtPath in SwitchArchive.ReadNames(archive).Where(x => x.EndsWith(".msbt", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var entry in MsbtMessageReader.Read(SwitchArchive.Extract(archive, msbtPath)))
                    messages.Add(new OdysseyMessage(Path.GetRelativePath(romFsPath, archivePath), msbtPath, entry.Key, entry.Value));
            }
        }

        return messages;
    }
}