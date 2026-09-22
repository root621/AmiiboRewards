using System.Buffers.Binary;
using System.Text;

namespace AmiiboRewards.Domain;

/// <summary>Shared Nintendo MSBT reader used by localization providers.</summary>
public static class MsbtMessageReader
{
    public static IReadOnlyDictionary<string, string> Read(ReadOnlySpan<byte> data)
    {
        if (SwitchFormatDetector.Detect(data) != SwitchDataFormat.Msbt || data.Length < 0x20) throw new InvalidDataException("Expected MSBT.");
        var little = data[8] == 0xFF && data[9] == 0xFE; var labels = new Dictionary<int, string>(); var texts = new Dictionary<int, string>();
        var sections = U16(data, 0x0E, little); var offset = 0x20;
        for (var section = 0; section < sections; section++)
        {
            Need(data, offset, 0x10); var tag = Encoding.ASCII.GetString(data.Slice(offset, 4)); var length = checked((int)U32(data, offset + 4, little)); var content = offset + 0x10; Need(data, content, length);
            if (tag == "LBL1") Labels(data.Slice(content, length), little, labels);
            else if (tag == "TXT2") Texts(data.Slice(content, length), little, texts);
            offset = (content + length + 15) & ~15;
        }
        return labels.Where(x => texts.ContainsKey(x.Key)).ToDictionary(x => x.Value, x => texts[x.Key], StringComparer.Ordinal);
    }
    private static void Labels(ReadOnlySpan<byte> data, bool little, IDictionary<int, string> target)
    {
        Need(data, 0, 4); var groups = checked((int)U32(data, 0, little)); Need(data, 4, checked(groups * 8));
        for (var group = 0; group < groups; group++)
        {
            var at = 4 + group * 8; var count = checked((int)U32(data, at, little)); var cursor = checked((int)U32(data, at + 4, little));
            for (var entry = 0; entry < count; entry++) { Need(data, cursor, 1); var length = data[cursor++]; Need(data, cursor, length + 4); var name = Encoding.UTF8.GetString(data.Slice(cursor, length)); cursor += length; target[checked((int)U32(data, cursor, little))] = name; cursor += 4; }
        }
    }
    private static void Texts(ReadOnlySpan<byte> data, bool little, IDictionary<int, string> target)
    {
        Need(data, 0, 4); var count = checked((int)U32(data, 0, little)); Need(data, 4, checked(count * 4));
        for (var i = 0; i < count; i++) { var cursor = checked((int)U32(data, 4 + i * 4, little)); if (cursor >= 4 + count * 4 && cursor < data.Length) target[i] = Text(data, cursor, little); }
    }
    private static string Text(ReadOnlySpan<byte> data, int cursor, bool little)
    {
        var value = new StringBuilder(); while (cursor + 2 <= data.Length) { var code = U16(data, cursor, little); cursor += 2; if (code == 0) break; if (code == 0x000E) { Need(data, cursor, 6); var payload = U16(data, cursor + 4, little); cursor += 6 + payload; continue; } value.Append((char)code); } return value.ToString().Replace("\r\n", "\n").Trim();
    }
    private static ushort U16(ReadOnlySpan<byte> data, int at, bool little) => little ? BinaryPrimitives.ReadUInt16LittleEndian(data[at..]) : BinaryPrimitives.ReadUInt16BigEndian(data[at..]);
    private static uint U32(ReadOnlySpan<byte> data, int at, bool little) => little ? BinaryPrimitives.ReadUInt32LittleEndian(data[at..]) : BinaryPrimitives.ReadUInt32BigEndian(data[at..]);
    private static void Need(ReadOnlySpan<byte> data, int at, int length) { if (at < 0 || length < 0 || at > data.Length - length) throw new InvalidDataException("Invalid MSBT data."); }
}
