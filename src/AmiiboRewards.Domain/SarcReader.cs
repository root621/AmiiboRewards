using System.Buffers.Binary;
namespace AmiiboRewards.Domain;
public static class SarcReader
{
    public static byte[] Extract(ReadOnlySpan<byte> data, string name)
    {
        var entries = ReadEntries(data); var entry = entries.SingleOrDefault(x => x.Name == name) ?? throw new FileNotFoundException($"SARC entry not found: {name}");
        return data.Slice(entry.Start, entry.End - entry.Start).ToArray();
    }
    public static IReadOnlyList<string> ReadNames(ReadOnlySpan<byte> data)
    {
        return ReadEntries(data).Select(x => x.Name).ToList();
    }
    private static IReadOnlyList<Entry> ReadEntries(ReadOnlySpan<byte> data)
    {
        if (data.Length < 0x20 || !data[..4].SequenceEqual("SARC"u8)) throw new InvalidDataException("Expected a SARC archive.");
        var little = data[6] == 0xFF && data[7] == 0xFE;
        var dataOffset = checked((int)Read32(data, 0x0C, little)); if (!data.Slice(0x14, 4).SequenceEqual("SFAT"u8)) throw new InvalidDataException("SARC is missing SFAT.");
        var count = little ? BinaryPrimitives.ReadUInt16LittleEndian(data[0x1A..]) : BinaryPrimitives.ReadUInt16BigEndian(data[0x1A..]);
        var sfnt = 0x20 + count * 0x10;
        if (sfnt + 8 > data.Length || !data.Slice(sfnt, 4).SequenceEqual("SFNT"u8)) throw new InvalidDataException("SARC is missing SFNT.");
        var strings = sfnt + 8; var result = new List<Entry>(count);
        for (var i = 0; i < count; i++) { var node = 0x20 + i * 0x10; var attributes = Read32(data, node + 4, little); var name = $"<unnamed:{i}>"; if ((attributes & 0x01000000) != 0) { var nameStart = strings + (int)(attributes & 0x00FFFFFF) * 4; if (nameStart >= data.Length) throw new InvalidDataException("Invalid SARC name offset."); var end = nameStart; while (end < data.Length && data[end] != 0) end++; name = System.Text.Encoding.UTF8.GetString(data[nameStart..end]); } var start = dataOffset + checked((int)Read32(data, node + 8, little)); var endData = dataOffset + checked((int)Read32(data, node + 12, little)); if (start < dataOffset || endData < start || endData > data.Length) throw new InvalidDataException("Invalid SARC data range."); result.Add(new Entry(name, start, endData)); }
        return result;
    }
    private static uint Read32(ReadOnlySpan<byte> data, int offset, bool little) => little ? BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]) : BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
    private sealed record Entry(string Name, int Start, int End);
}
