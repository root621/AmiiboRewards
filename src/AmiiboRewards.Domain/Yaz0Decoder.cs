namespace AmiiboRewards.Domain;

public static class Yaz0Decoder
{
    public static byte[] Decode(ReadOnlySpan<byte> source)
    {
        if (source.Length < 16 || !source[..4].SequenceEqual("Yaz0"u8)) throw new InvalidDataException("The input is not a Yaz0 stream.");
        var size = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(source[4..8]);
        if (size < 0) throw new InvalidDataException("Invalid Yaz0 output size.");
        var output = new byte[size]; var input = 16; var outputIndex = 0; var code = 0; var bits = 0;
        while (outputIndex < size)
        {
            if (bits == 0) { if (input >= source.Length) throw new InvalidDataException("Unexpected end of Yaz0 stream."); code = source[input++]; bits = 8; }
            if ((code & 0x80) != 0) { if (input >= source.Length) throw new InvalidDataException("Unexpected end of Yaz0 literal."); output[outputIndex++] = source[input++]; }
            else
            {
                if (input + 1 >= source.Length) throw new InvalidDataException("Unexpected end of Yaz0 back-reference.");
                var first = source[input++]; var second = source[input++]; var distance = ((first & 0x0F) << 8) | second; var length = first >> 4;
                if (length == 0) { if (input >= source.Length) throw new InvalidDataException("Unexpected end of Yaz0 length."); length = source[input++] + 0x12; } else length += 2;
                var copy = outputIndex - (distance + 1); if (copy < 0) throw new InvalidDataException("Invalid Yaz0 back-reference.");
                for (var i = 0; i < length && outputIndex < size; i++) output[outputIndex++] = output[copy++];
            }
            code <<= 1; bits--;
        }
        return output;
    }
}
