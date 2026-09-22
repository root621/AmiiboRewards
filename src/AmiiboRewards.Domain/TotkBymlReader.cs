using System.Buffers.Binary;
using System.Text;

namespace AmiiboRewards.Domain;

/// <summary>
/// Small, read-only BYML reader for Nintendo's v7 little-endian data. It deliberately
/// exposes plain dictionaries/lists because game readers should own their schemas.
/// </summary>
public static class BymlReader
{
    public static object? ReadAny(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 0x10 || !bytes[..2].SequenceEqual("YB"u8)) throw new InvalidDataException("Expected little-endian BYML.");
        var version = BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]);
        if (version is not (3 or 7)) throw new InvalidDataException($"Unsupported BYML version {version}.");
        var keys = ReadStringTable(bytes, Offset(bytes, 4));
        var strings = ReadStringTable(bytes, Offset(bytes, 8));
        return ReadNode(bytes, Offset(bytes, 12), keys, strings);
    }

    public static IReadOnlyDictionary<string, object?> Read(ReadOnlySpan<byte> bytes)
    {
        var root = ReadAny(bytes);
        return root as IReadOnlyDictionary<string, object?> ?? throw new InvalidDataException("BYML root is not a hash.");
    }

    private static object? ReadNode(ReadOnlySpan<byte> data, int offset, IReadOnlyList<string> keys, IReadOnlyList<string> strings)
    {
        Check(data, offset, 4); var type = data[offset]; var count = Read24(data, offset + 1);
        return type switch
        {
            0xFF => null,
            0xD0 => data[offset + 4] != 0,
            0xD1 => BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset + 4, 4)),
            0xD2 => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset + 4, 4))),
            0xD3 => BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, 4)),
            0xD4 => BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset + 4, 8)),
            0xD5 => BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset + 4, 8)),
            0xD6 => BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(data.Slice(offset + 4, 8))),
            0xA0 => String(strings, BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, 4))),
            0xC0 => ReadArray(data, offset, count, keys, strings),
            0xC1 => ReadHash(data, offset, count, keys, strings),
            _ => throw new InvalidDataException($"Unsupported BYML node type 0x{type:X2}.")
        };
    }

    private static IReadOnlyList<object?> ReadArray(ReadOnlySpan<byte> data, int offset, int count, IReadOnlyList<string> keys, IReadOnlyList<string> strings)
    {
        var typesStart = offset + 4; Check(data, typesStart, count); var valuesStart = Align4(typesStart + count); Check(data, valuesStart, checked(count * 4));
        var result = new List<object?>(count);
        for (var i = 0; i < count; i++) result.Add(ReadValue(data, data[typesStart + i], valuesStart + i * 4, keys, strings));
        return result;
    }

    private static IReadOnlyDictionary<string, object?> ReadHash(ReadOnlySpan<byte> data, int offset, int count, IReadOnlyList<string> keys, IReadOnlyList<string> strings)
    {
        var start = offset + 4; Check(data, start, checked(count * 8)); var result = new Dictionary<string, object?>(count, StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            var entry = start + i * 8; var key = String(keys, (uint)Read24(data, entry));
            result[key] = ReadValue(data, data[entry + 3], entry + 4, keys, strings);
        }
        return result;
    }

    private static object? ReadValue(ReadOnlySpan<byte> data, byte type, int offset, IReadOnlyList<string> keys, IReadOnlyList<string> strings)
    {
        Check(data, offset, 4); var raw = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
        return type switch
        {
            0xFF => null,
            0xD0 => raw != 0,
            0xD1 => unchecked((int)raw),
            0xD2 => BitConverter.Int32BitsToSingle(unchecked((int)raw)),
            0xD3 => raw,
            0xA0 => String(strings, raw),
            0xC0 or 0xC1 => ReadNode(data, checked((int)raw), keys, strings),
            _ => throw new InvalidDataException($"Unsupported BYML value type 0x{type:X2}.")
        };
    }

    private static IReadOnlyList<string> ReadStringTable(ReadOnlySpan<byte> data, int offset)
    {
        Check(data, offset, 4); if (data[offset] != 0xC2) throw new InvalidDataException("BYML string table is invalid.");
        var count = Read24(data, offset + 1); Check(data, offset + 4, checked((count + 1) * 4)); var result = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var start = offset + checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4 + i * 4, 4)));
            var end = data[start..].IndexOf((byte)0); if (end < 0) throw new InvalidDataException("Unterminated BYML string.");
            result.Add(Encoding.UTF8.GetString(data.Slice(start, end)));
        }
        return result;
    }
    private static int Offset(ReadOnlySpan<byte> data, int at) { Check(data, at, 4); return checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(at, 4))); }
    private static int Read24(ReadOnlySpan<byte> data, int at) { Check(data, at, 3); return data[at] | data[at + 1] << 8 | data[at + 2] << 16; }
    private static string String(IReadOnlyList<string> strings, uint index) => index < strings.Count ? strings[(int)index] : throw new InvalidDataException("Invalid BYML string index.");
    private static int Align4(int value) => (value + 3) & ~3;
    private static void Check(ReadOnlySpan<byte> data, int offset, int length) { if (offset < 0 || length < 0 || offset > data.Length - length) throw new InvalidDataException("Truncated BYML data."); }
}
