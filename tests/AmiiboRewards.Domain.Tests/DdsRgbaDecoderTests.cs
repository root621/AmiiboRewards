using System.Buffers.Binary;
using AmiiboRewards.Domain;

namespace AmiiboRewards.Domain.Tests;

public sealed class DdsRgbaDecoderTests
{
    private static byte[] Fixture(bool bgra = false)
    {
        var data = new byte[132]; "DDS "u8.CopyTo(data);
        void Put(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);
        Put(4, 124); Put(8, 8); Put(12, 1); Put(16, 1); Put(20, 4); Put(76, 32); Put(80, 0x41); Put(88, 32);
        Put(92, bgra ? 0xff0000u : 0xffu); Put(96, 0xff00); Put(100, bgra ? 0xffu : 0xff0000u); Put(104, 0xff000000);
        data[128] = bgra ? (byte)94 : (byte)218; data[129] = 162; data[130] = bgra ? (byte)218 : (byte)94; data[131] = 128;
        return data;
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Preserves_red_blue_and_alpha_using_header_masks(bool bgra) => Assert.Equal(new byte[] { 218, 162, 94, 128 }, DdsRgbaDecoder.TryDecode(Fixture(bgra))!.Pixels);
    [Fact] public void Rejects_truncated_pixels() => Assert.Throws<InvalidDataException>(() => DdsRgbaDecoder.TryDecode(Fixture()[..130]));
    [Fact] public void Compressed_formats_are_delegated() { var data = Fixture(); data[80] = 4; Assert.Null(DdsRgbaDecoder.TryDecode(data)); }
}
