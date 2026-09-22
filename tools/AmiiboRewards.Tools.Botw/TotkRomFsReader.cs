using System.Security.Cryptography;
using AmiiboRewards.Domain;

namespace AmiiboRewards.Tools.Botw;

internal static class TotkRomFsReader
{
    public const string SettingsPath = "Game/AmiiboSetting/AmiiboSetting.game__ui__AmiiboSetting.bgyml";
    public const string ResidentArchive = "Pack/ResidentCommon.pack.zs";

    public static async Task<TotkRomFsData> ReadAsync(string romFsPath, CancellationToken ct = default)
        => await ReadAsync(GameProfiles.Totk, RomFsIndex.Create(romFsPath), ct);

    public static async Task<TotkRomFsData> ReadAsync(GameProfile profile, RomFsIndex index, CancellationToken ct = default)
    {
        if (!profile.SemanticSourcePaths.Any(x => x.StartsWith(ResidentArchive, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException($"Profile {profile.Code} does not declare the TOTK semantic source.");
        var zsdic = Path.Combine(index.RootPath, "Pack", "ZsDic.pack.zs");
        var resident = Path.Combine(index.RootPath, ResidentArchive.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(zsdic) || !File.Exists(resident)) throw new FileNotFoundException("TOTK requires Pack/ZsDic.pack.zs and Pack/ResidentCommon.pack.zs.");
        var temp = Directory.CreateTempSubdirectory("amiibo-totk-");
        try
        {
            // ZsDic contains multiple dictionaries. ResidentCommon is a pack stream, not the generic .zs stream used by album JPEGs.
            var dictionary = Path.Combine(temp.FullName, "pack.zsdic");
            await File.WriteAllBytesAsync(dictionary, await ZstdDictionaryReader.ReadAsync(zsdic, "pack.zsdic", ct), ct);
            var archive = Path.Combine(temp.FullName, "resident.sarc");
            await ZstdReader.DecompressAsync(resident, archive, dictionary, ct);
            var source = await File.ReadAllBytesAsync(archive, ct);
            var byml = SwitchArchive.Extract(source, SettingsPath);
            return new(TotkAmiiboSettingsReader.Read(byml), Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(resident, ct))).ToLowerInvariant());
        }
        finally { temp.Delete(true); }
    }

}

internal sealed record TotkRomFsData(TotkAmiiboSettings Settings, string SourceSha256);
