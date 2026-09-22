using System.Buffers.Binary;
using System.Numerics;

namespace AmiiboRewards.Domain;

public sealed record RgbaImage(int Width, int Height, byte[] Pixels);

public static class DdsRgbaDecoder
{
    // ImageMagick's DDS reader can ignore RGBA masks and interpret bytes as BGRA.
    // Decode uncompressed 32-bit DDS explicitly; send other formats to its normal reader.
    public static RgbaImage? TryDecode(ReadOnlySpan<byte> dds)
    {
        if (dds.Length < 128 || !dds[..4].SequenceEqual("DDS "u8)) throw new InvalidDataException("Invalid DDS header.");
        if (Read(dds, 4) != 124 || Read(dds, 76) != 32) throw new InvalidDataException("Invalid DDS header size.");
        var flags = Read(dds, 80);
        if ((flags & 0x40) == 0 || (flags & 4) != 0 || Read(dds, 88) != 32) return null;
        var width = checked((int)Read(dds, 16)); var height = checked((int)Read(dds, 12));
        if (width < 1 || height < 1 || width > 8192 || height > 8192) throw new InvalidDataException("Unsupported DDS dimensions.");
        var stride = (Read(dds, 8) & 8) != 0 ? checked((int)Read(dds, 20)) : checked(width * 4);
        if (stride < width * 4 || (long)stride * height > dds.Length - 128) throw new InvalidDataException("Truncated DDS pixels.");
        var masks = new[] { Read(dds, 92), Read(dds, 96), Read(dds, 100), Read(dds, 104) };
        for (var i = 0; i < masks.Length; i++)
        {
            if (i < 3 && masks[i] == 0) throw new InvalidDataException("Missing DDS color mask.");
            if (masks[i] != 0 && (masks[i] >> BitOperations.TrailingZeroCount(masks[i])) != 255) throw new InvalidDataException("Expected 8-bit DDS channel masks.");
            for (var j = 0; j < i; j++) if ((masks[i] & masks[j]) != 0) throw new InvalidDataException("Overlapping DDS channel masks.");
        }
        var pixels = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
        {
            var pixel = Read(dds, 128 + y * stride + x * 4);
            for (var c = 0; c < 4; c++) pixels[(y * width + x) * 4 + c] = masks[c] == 0 ? (byte)255 : (byte)((pixel & masks[c]) >> BitOperations.TrailingZeroCount(masks[c]));
        }
        return new(width, height, pixels);
    }

    private static uint Read(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
}
