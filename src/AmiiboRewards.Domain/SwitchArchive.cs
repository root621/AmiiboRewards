namespace AmiiboRewards.Domain;

/// <summary>Common archive boundary used by game adapters; format implementations stay below this API.</summary>
public static class SwitchArchive
{
    public static byte[] DecodeCompression(ReadOnlySpan<byte> data) => SwitchFormatDetector.Detect(data) switch
    {
        SwitchDataFormat.Yaz0 => Yaz0Decoder.Decode(data),
        _ => data.ToArray()
    };

    public static IReadOnlyList<string> ReadNames(ReadOnlySpan<byte> data) => SarcReader.ReadNames(data);
    public static byte[] Extract(ReadOnlySpan<byte> data, string relativePath) => SarcReader.Extract(data, relativePath);

    public static IReadOnlyDictionary<string, string> ReadMessageCatalog(ReadOnlySpan<byte> sarc)
    {
        var messages = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in ReadNames(sarc).Where(x => x.EndsWith(".msbt", StringComparison.OrdinalIgnoreCase)))
            foreach (var message in MsbtMessageReader.Read(Extract(sarc, name)))
                messages.TryAdd(message.Key, message.Value);
        return messages;
    }
}