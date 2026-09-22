using System.Buffers.Binary;

namespace AmiiboRewards.Domain;

/// <summary>Identity encoded in an unencrypted NTAG215 amiibo dump.</summary>
public sealed record AmiiboIdentity(byte[] CharacterId, byte SeriesId, ushort NumberingId, byte NfpType, byte Version)
{
    public byte[] CharacterBaseId => CharacterId.Length >= 2 ? CharacterId[..2] : [];
    public ushort CharacterIdAsUInt16 => CharacterId.Length >= 2 ? BinaryPrimitives.ReadUInt16LittleEndian(CharacterId.AsSpan(0, 2)) : (ushort)0;
    public static AmiiboIdentity Read(ReadOnlySpan<byte> dump)
    {
        const int offset = 0x54;
        if (dump.Length < offset + 8) throw new InvalidDataException("The amiibo dump is shorter than the NTAG215 identity block.");
        return new AmiiboIdentity(dump.Slice(offset, 3).ToArray(), dump[offset + 3],
            BinaryPrimitives.ReadUInt16BigEndian(dump.Slice(offset + 4, 2)), dump[offset + 6], dump[offset + 7]);
    }
}
