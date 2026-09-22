using System.Buffers.Binary;
using System.Text;

namespace AmiiboRewards.Domain;

/// <summary>Bounds-checked reader for the shared AAMP binary container used by Switch games.</summary>
public static class AampDropTableReader
{
    public static IReadOnlyList<AampDropPool> ReadDropTable(ReadOnlySpan<byte> data)
    {
        if (SwitchFormatDetector.Detect(data) != SwitchDataFormat.Aamp || data.Length < 0x40 || !data.Slice(0x30, 4).SequenceEqual("xml\0"u8)) throw new InvalidDataException("Expected an AAMP binary.");
        var root = Node(data, 0x34); if (root.Count < 2) throw new InvalidDataException("AAMP drop table has no pool lists.");
        var names = List(root[0]).Skip(1).Select(Text).ToList(); var output = new List<AampDropPool>();
        for (var index = 1; index < root.Count && index <= names.Count; index++)
        {
            var values = List(root[index]); if (values.Count < 5) throw new InvalidDataException("Incomplete AAMP pool.");
            var count = UInt(values[4]); if (values.Count < 5 + count * 2) throw new InvalidDataException("AAMP pool reward count is invalid.");
            output.Add(new(names[index - 1], Enumerable.Range(0, checked((int)count)).Select(i => new AampDrop(Text(values[5 + i * 2]), Float(values[6 + i * 2]))).ToList()));
        }
        return output;
    }
    private static List<object> Node(ReadOnlySpan<byte> data, int offset)
    {
        Need(data, offset, 12); var nodes = offset + 4 * Read16(data, offset + 4); var nodeCount = data[offset + 6]; var lists = offset + 4 * Read16(data, offset + 8); var listCount = data[offset + 10];
        if (data[offset + 7] != 0 || data[offset + 11] != 0) throw new InvalidDataException("Unexpected AAMP child type.");
        for (var i = 0; i < nodeCount; i++) _ = Node(data, nodes + 12 * i);
        var result = new List<object>(listCount); for (var i = 0; i < listCount; i++) result.Add(ReadList(data, lists + 8 * i)); return result;
    }
    private static object ReadList(ReadOnlySpan<byte> data, int offset)
    {
        Need(data, offset, 8); var values = offset + 4 * Read16(data, offset + 4); var count = data[offset + 6]; var type = data[offset + 7];
        if (type == 0) { var result = new List<object>(count); for (var i = 0; i < count; i++) result.Add(ReadList(data, values + 8 * i)); return result; }
        if (count != 0) throw new InvalidDataException("AAMP typed list unexpectedly contains child lists.");
        return Value(data, values, type);
    }
    private static object Value(ReadOnlySpan<byte> data, int offset, byte type)
    {
        Need(data, offset, 4); var value = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
        return type switch { 1 or 4 or 6 => BitConverter.Int32BitsToSingle(unchecked((int)value)), 2 or 0x11 => value, 7 or 8 or 0x0F or 0x14 => String(data, offset), _ => throw new InvalidDataException($"Unsupported AAMP value type 0x{type:X2}.") };
    }
    private static List<object> List(object value) => value as List<object> ?? throw new InvalidDataException("Expected AAMP list.");
    private static string Text(object value) => value as string ?? throw new InvalidDataException("Expected AAMP string.");
    private static uint UInt(object value) => value is uint v ? v : throw new InvalidDataException("Expected AAMP unsigned integer.");
    private static float Float(object value) => value is float v ? v : throw new InvalidDataException("Expected AAMP float.");
    private static string String(ReadOnlySpan<byte> data, int offset) { var end = offset; while (end < data.Length && data[end] != 0) end++; if (end == data.Length) throw new InvalidDataException("Unterminated AAMP string."); return Encoding.ASCII.GetString(data[offset..end]); }
    private static ushort Read16(ReadOnlySpan<byte> data, int offset) { Need(data, offset, 2); return BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]); }
    private static void Need(ReadOnlySpan<byte> data, int offset, int length) { if (offset < 0 || length < 0 || offset > data.Length - length) throw new InvalidDataException("AAMP offset is outside input."); }
}
public sealed record AampDropPool(string Name, IReadOnlyList<AampDrop> Rewards);
public sealed record AampDrop(string ItemId, float Probability);
