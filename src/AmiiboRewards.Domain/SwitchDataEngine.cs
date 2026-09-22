namespace AmiiboRewards.Domain;

public enum SwitchDataFormat { Unknown, Yaz0, Sarc, Byml, Aamp, Msbt, Bntx, Zstd }

public static class SwitchFormatDetector
{
    public static SwitchDataFormat Detect(ReadOnlySpan<byte> data) =>
        data.Length >= 8 && data[..8].SequenceEqual("MsgStdBn"u8) ? SwitchDataFormat.Msbt :
        data.Length >= 4 && data[..4].SequenceEqual("Yaz0"u8) ? SwitchDataFormat.Yaz0 :
        data.Length >= 4 && data[..4].SequenceEqual("SARC"u8) ? SwitchDataFormat.Sarc :
        data.Length >= 4 && data[..4].SequenceEqual("AAMP"u8) ? SwitchDataFormat.Aamp :
        data.Length >= 4 && data[..4].SequenceEqual("BNTX"u8) ? SwitchDataFormat.Bntx :
        data.Length >= 2 && data[..2].SequenceEqual("YB"u8) ? SwitchDataFormat.Byml :
        data.Length >= 4 && data[..4].SequenceEqual(new byte[] { 0x28, 0xB5, 0x2F, 0xFD }) ? SwitchDataFormat.Zstd : SwitchDataFormat.Unknown;

    public static byte[] DecodeYaz0IfNeeded(ReadOnlySpan<byte> data) => Detect(data) == SwitchDataFormat.Yaz0 ? Yaz0Decoder.Decode(data) : data.ToArray();
}

public sealed record RomFsFile(string RelativePath, string FileName, string Extension, long Size, SwitchDataFormat Format);

/// <summary>Lazy metadata index; it never decompresses archives while walking a RomFS.</summary>
public sealed class RomFsIndex
{
    private readonly IReadOnlyList<RomFsFile> files;
    public string RootPath { get; }
    private RomFsIndex(string rootPath, IReadOnlyList<RomFsFile> files) { RootPath = rootPath; this.files = files; }
    public IReadOnlyList<RomFsFile> Files => files;
    public static RomFsIndex Create(string romFsPath)
    {
        if (!Directory.Exists(romFsPath)) throw new DirectoryNotFoundException($"RomFS directory not found: {romFsPath}");
        var root = Path.GetFullPath(romFsPath); var indexed = new List<RomFsFile>();
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            var info = new FileInfo(path); var format = DetectCheap(path);
            indexed.Add(new(relative, Path.GetFileName(path), Path.GetExtension(path), info.Length, format));
        }
        return new(root, indexed);
    }
    public IEnumerable<RomFsFile> FindByName(string name) => files.Where(x => x.FileName.Equals(name, StringComparison.OrdinalIgnoreCase));
    public IEnumerable<RomFsFile> FindByExtension(string extension) => files.Where(x => x.Extension.Equals(extension.StartsWith('.') ? extension : "." + extension, StringComparison.OrdinalIgnoreCase));
    public IEnumerable<RomFsFile> FindContainingPathText(string text) => files.Where(x => x.RelativePath.Contains(text, StringComparison.OrdinalIgnoreCase));
    public IEnumerable<RomFsFile> FindByNameFragment(string fragment) => files.Where(x => x.FileName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    public IEnumerable<RomFsFile> FindByPathFragment(string fragment) => files.Where(x => x.RelativePath.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    public IEnumerable<RomFsFile> FindCandidates(params string[] keywords) => files.Where(x => keywords.Any(keyword => x.RelativePath.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    public IEnumerable<RomFsFile> FindPossibleAmiiboFiles() => FindCandidates("amiibo", "nfp", "figure");
    public IEnumerable<RomFsFile> FindPossibleLocalizationArchives() => files.Where(x => x.RelativePath.Contains("message", StringComparison.OrdinalIgnoreCase) || x.RelativePath.Contains("mals/", StringComparison.OrdinalIgnoreCase) || x.Format == SwitchDataFormat.Msbt);
    public IEnumerable<RomFsFile> FindPossibleTextureAssets() => files.Where(x => x.Format == SwitchDataFormat.Bntx || x.RelativePath.Contains("texture", StringComparison.OrdinalIgnoreCase) || x.RelativePath.Contains("stockitem", StringComparison.OrdinalIgnoreCase));
    public string FullPath(RomFsFile file) => Path.Combine(RootPath, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
    private static SwitchDataFormat DetectCheap(string path)
    {
        try { using var stream = File.OpenRead(path); Span<byte> bytes = stackalloc byte[8]; var count = stream.Read(bytes); return SwitchFormatDetector.Detect(bytes[..count]); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return SwitchDataFormat.Unknown; }
    }
}

public sealed record GameProfile(string Code, string TitleId, string Name, string ShortName, string Theme,
    IReadOnlyList<string> RequiredRomFsMarkers, string AdapterId, IReadOnlyList<string> LocalizationPatterns, IReadOnlyList<string> AssetPathHints)
{
    public IReadOnlyList<string> SemanticSourcePaths { get; init; } = [];
    public IReadOnlyList<string> LocalizationGroupHints { get; init; } = [];
}

public static class GameProfiles
{
    public static readonly GameProfile Botw = new("BOTW", "01007EF00011E000", "The Legend of Zelda: Breath of the Wild", "Breath of the Wild", "forest",
        ["Actor/Pack"], "botw-aamp", ["Pack/Bootup_{locale}.pack"], ["UI/StockItem"])
    { SemanticSourcePaths = ["Actor/Pack/Item_Amiibo_DropTable_*.sbactorpack"] };
    public static readonly GameProfile Totk = new("TOTK", "0100F2C0115B6000", "The Legend of Zelda: Tears of the Kingdom", "Tears of the Kingdom", "ruins",
        ["Pack/ZsDic.pack.zs", "Pack/ResidentCommon.pack.zs"], "totk-byml", ["Mals/{locale}.Product.*.sarc.zs"], ["UI", "TexToGo"])
    { SemanticSourcePaths = ["Pack/ResidentCommon.pack.zs::Game/AmiiboSetting/AmiiboSetting.game__ui__AmiiboSetting.bgyml"], LocalizationGroupHints = ["ActorMsg", "ActorMsg/PouchContent", "ActorMsg/PictureBook"] };
    public static readonly IReadOnlyList<GameProfile> All = [Botw, Totk];
    public static GameProfile? Find(string code) => All.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
}

public enum InteractionKind { Drop, Unlock, Spawn, Special }
public enum OutcomeKind { Item, ActorSpawn, Container, CostumeUnlock, CharacterUnlock, ModeUnlock, Event, Companion, Special }
/// <summary>Game-agnostic data emitted by semantic adapters and persisted through the existing reward tables.</summary>
public sealed record GameInteraction(string SelectorId, InteractionKind InteractionKind, OutcomeKind OutcomeKind, string SourceId,
    string Pool, decimal? RawWeight, decimal? NormalizedProbability, int? MinCount, int? MaxCount, string? Condition,
    string? SpecialMetadata, string SourcePath, string SourceHash, int SourceOrder);

public interface IGameInteractionAdapter
{
    string Id { get; }
    bool Supports(GameProfile profile, RomFsIndex index);
    Task<IReadOnlyList<GameInteraction>> ReadAsync(GameProfile profile, RomFsIndex index, CancellationToken cancellationToken = default);
}

public interface IGameAssetResolver { Task<string?> ResolveAsync(GameProfile profile, RomFsIndex index, string sourceId, CancellationToken cancellationToken = default); }
