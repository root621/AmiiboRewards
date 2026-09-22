namespace AmiiboRewards.Domain;

public sealed record GameDefinition(string Code, string Name, string ShortName, string Theme, bool CanImport);
public sealed record DetectedRomFs(GameDefinition Game, string RomFsPath);
public sealed record DumpScan(IReadOnlyList<DetectedRomFs> Detected, IReadOnlyList<string> Warnings);

// A game's identity is independent from whether its reward format has an importer.
public static class GameDefinitions
{
    public static readonly GameDefinition Botw = new(GameProfiles.Botw.Code, GameProfiles.Botw.Name, GameProfiles.Botw.ShortName, GameProfiles.Botw.Theme, true);
    public static readonly GameDefinition Totk = new(GameProfiles.Totk.Code, GameProfiles.Totk.Name, GameProfiles.Totk.ShortName, GameProfiles.Totk.Theme, true);
    public static readonly GameDefinition Odyssey = new(GameProfiles.Odyssey.Code, GameProfiles.Odyssey.Name, GameProfiles.Odyssey.ShortName, GameProfiles.Odyssey.Theme, true);
    public static GameDefinition? Find(string code) => code.ToUpperInvariant() switch { "BOTW" => Botw, "TOTK" => Totk, "ODYSSEY" => Odyssey, _ => null };

    public static DetectedRomFs? Detect(string directory)
    {
        var root = Directory.Exists(Path.Combine(directory, "romfs")) ? Path.Combine(directory, "romfs") : directory;
        if (!Directory.Exists(root)) return null;
        var botw = Path.Combine(root, "Actor", "Pack");
        if (Directory.Exists(botw) && Directory.EnumerateFiles(botw, "Item_Amiibo_DropTable_*.sbactorpack").Any())
            return new(Botw, Path.GetFullPath(root));
        if (GameProfiles.Totk.RequiredRomFsMarkers.All(marker => File.Exists(Path.Combine(root, marker.Replace('/', Path.DirectorySeparatorChar)))))
            return new(Totk, Path.GetFullPath(root));
        if (GameProfiles.Odyssey.RequiredRomFsMarkers.All(marker => File.Exists(Path.Combine(root, marker.Replace('/', Path.DirectorySeparatorChar)))))
            return new(Odyssey, Path.GetFullPath(root));
        return null;
    }

    public static DumpScan Scan(string root)
    {
        var found = new List<DetectedRomFs>(); var warnings = new List<string>();
        if (!Directory.Exists(root)) return new(found, ["La carpeta configurada no existe o no está disponible."]);
        void Inspect(string path)
        {
            try
            {
                var item = Detect(path);
                if (item is not null) found.Add(item);
                else warnings.Add($"No se reconoció una RomFS compatible en {Path.GetFileName(path)}; puede estar incompleta.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { warnings.Add($"No se pudo leer {Path.GetFileName(path)}."); }
        }
        try
        {
            if (Detect(root) is { } direct) found.Add(direct);
            else foreach (var path in Directory.EnumerateDirectories(root).OrderBy(x => x, StringComparer.Ordinal)) Inspect(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { warnings.Add("No se pudo leer la carpeta configurada."); }
        if (found.Count == 0 && warnings.Count == 0) warnings.Add("No se encontraron juegos. Agregá las RomFS extraídas y volvé a explorar.");
        return new(found, warnings);
    }
}
