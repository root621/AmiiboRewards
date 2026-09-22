using AmiiboRewards.Domain;
namespace AmiiboRewards.Domain.Tests;
public sealed class Yaz0DecoderTests { [Fact] public void Decodes_literals() => Assert.Equal("ABC", System.Text.Encoding.ASCII.GetString(Yaz0Decoder.Decode([.. "Yaz0"u8, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0b11100000, (byte)'A', (byte)'B', (byte)'C']))); [Theory] [InlineData("Animal_Fish_A", "Item_FishGet_A")] [InlineData("Animal_Fish_X", "Item_FishGet_X")] [InlineData("Weapon_Bow_017", "Weapon_Bow_017")] public void Resolves_aliases(string input, string expected) => Assert.Equal(expected, FishIconAliases.Resolve(input)); }
