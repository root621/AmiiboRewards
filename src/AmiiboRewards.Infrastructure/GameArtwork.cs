using AmiiboRewards.Domain;

namespace AmiiboRewards.Infrastructure;

public static class GameArtwork
{
    public static async Task SaveAsync(DetectedRomFs detected, string assetsRoot, CancellationToken ct)
    {
        var output = Path.Combine(assetsRoot, detected.Game.Code.ToLowerInvariant(), "cover.jpg");
        if (File.Exists(output)) return;
        var custom = Path.Combine(detected.RomFsPath, "cover.jpg");
        var plain = detected.Game.Code == "BOTW" ? Path.Combine(detected.RomFsPath, "UI", "Album", "pict_000.jpg") : custom;
        if (File.Exists(custom)) plain = custom;
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        if (File.Exists(plain)) { File.Copy(plain, output); return; }
        if (detected.Game.Code != "TOTK") return;
        var album = Path.Combine(detected.RomFsPath, "UI", "Album", "Default_00_Photo.jpg.zs");
        var dictionaries = Path.Combine(detected.RomFsPath, "Pack", "ZsDic.pack.zs");
        if (!File.Exists(album) || !File.Exists(dictionaries)) return;
        var temp = Directory.CreateTempSubdirectory("amiibo-art-");
        try
        {
            var sarc = Path.Combine(temp.FullName, "dictionaries.sarc");
            await ZstdReader.DecompressAsync(dictionaries, sarc, cancellationToken: ct);
            var dict = Path.Combine(temp.FullName, "zs.zsdic");
            await File.WriteAllBytesAsync(dict, SarcReader.Extract(await File.ReadAllBytesAsync(sarc, ct), "zs.zsdic"), ct);
            var image = Path.Combine(temp.FullName, "cover.jpg");
            await ZstdReader.DecompressAsync(album, image, dict, ct);
            var bytes = await File.ReadAllBytesAsync(image, ct);
            if (bytes.Length < 3 || bytes[0] != 0xff || bytes[1] != 0xd8 || bytes[2] != 0xff) throw new InvalidDataException("El recurso extraído no es JPEG.");
            File.Copy(image, output);
        }
        finally { temp.Delete(recursive: true); }
    }
}
