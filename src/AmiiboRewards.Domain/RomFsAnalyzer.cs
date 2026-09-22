namespace AmiiboRewards.Domain;

public sealed record RomFsAnalysisReport(
    GameDefinition? Game,
    int FileCount,
    IReadOnlyDictionary<SwitchDataFormat, int> FormatCounts,
    IReadOnlyDictionary<string, IReadOnlyList<RomFsFile>> KeywordMatches,
    IReadOnlyList<RomFsFile> BymlCandidates,
    IReadOnlyList<RomFsFile> AampCandidates,
    IReadOnlyList<RomFsFile> MsbtCandidates,
    IReadOnlyList<RomFsFile> LocalizationCandidates,
    IReadOnlyList<RomFsFile> TextureCandidates,
    IReadOnlyList<RomFsFile> SarcCandidates);

public static class RomFsAnalyzer
{
    private static readonly string[] Keywords = ["amiibo", "nfp", "figure", "reward", "drop", "costume", "unlock"];

    public static RomFsAnalysisReport Analyze(RomFsIndex index)
    {
        var files = index.Files;
        var keywords = Keywords.ToDictionary(keyword => keyword, keyword => files.Where(file => file.RelativePath.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList() as IReadOnlyList<RomFsFile>, StringComparer.OrdinalIgnoreCase);
        return new(
            GameDefinitions.Detect(index.RootPath)?.Game,
            files.Count,
            Enum.GetValues<SwitchDataFormat>().ToDictionary(format => format, format => files.Count(file => file.Format == format)),
            keywords,
            Find(files, ".byml", ".bgyml", ".byml.zs", ".bgyml.zs"),
            Find(files, ".aamp", ".bdrop", ".aamp.zs", ".bdrop.zs"),
            Find(files, ".msbt", ".msbt.zs"),
            files.Where(file => file.RelativePath.Contains("message", StringComparison.OrdinalIgnoreCase) || file.RelativePath.Contains("mals/", StringComparison.OrdinalIgnoreCase) || file.FileName.EndsWith(".msbt", StringComparison.OrdinalIgnoreCase) || file.FileName.Contains("Product.", StringComparison.OrdinalIgnoreCase)).ToList(),
            files.Where(file => file.Format == SwitchDataFormat.Bntx || Has(file, ".bntx", ".bntx.zs", ".txtg", ".bfres", ".bfres.zs") || file.RelativePath.Contains("texture", StringComparison.OrdinalIgnoreCase) || file.RelativePath.Contains("stockitem", StringComparison.OrdinalIgnoreCase)).ToList(),
            files.Where(file => file.Format == SwitchDataFormat.Sarc || Has(file, ".sarc", ".ssarc", ".pack", ".sbactorpack", ".pack.zs", ".sarc.zs", ".ssarc.zs")).ToList());
    }

    private static IReadOnlyList<RomFsFile> Find(IReadOnlyList<RomFsFile> files, params string[] suffixes) => files.Where(file => Has(file, suffixes)).ToList();
    private static bool Has(RomFsFile file, params string[] suffixes) => suffixes.Any(suffix => file.FileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
}