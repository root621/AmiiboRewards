using AmiiboRewards.Domain;

namespace AmiiboRewards.Domain.Tests;

public sealed class SwitchDataEngineTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "amiibo-index-" + Guid.NewGuid());
    [Fact]
    public void Detects_known_format_magics()
    {
        Assert.Equal(SwitchDataFormat.Yaz0, SwitchFormatDetector.Detect("Yaz0"u8));
        Assert.Equal(SwitchDataFormat.Sarc, SwitchFormatDetector.Detect("SARC"u8));
        Assert.Equal(SwitchDataFormat.Byml, SwitchFormatDetector.Detect("YB"u8));
        Assert.Equal(SwitchDataFormat.Aamp, SwitchFormatDetector.Detect("AAMP"u8));
        Assert.Equal(SwitchDataFormat.Msbt, SwitchFormatDetector.Detect("MsgStdBn"u8));
    }
    [Fact]
    public void Indexes_without_decompressing_and_queries_candidates()
    {
        Directory.CreateDirectory(Path.Combine(root, "Pack")); File.WriteAllBytes(Path.Combine(root, "Pack", "AmiiboSetting.bgyml"), "YB"u8.ToArray()); File.WriteAllBytes(Path.Combine(root, "Pack", "CostumeUnlock.bgyml"), "YB"u8.ToArray());
        var index = RomFsIndex.Create(root);
        Assert.Single(index.FindPossibleAmiiboFiles()); Assert.Equal(2, index.FindCandidates("amiibo", "costume").Count()); Assert.Equal(SwitchDataFormat.Byml, index.Files.Single(x => x.FileName.StartsWith("Amiibo")).Format);
    }
    [Fact] public void Profiles_describe_game_specific_sources_without_absolute_paths() { Assert.Contains("ResidentCommon", GameProfiles.Totk.SemanticSourcePaths.Single()); Assert.Equal("0100F2C0115B6000", GameProfiles.Totk.TitleId); Assert.Contains("Actor/Pack", GameProfiles.Botw.SemanticSourcePaths.Single()); }
    [Fact] public void Analyzer_reports_named_candidates_and_detects_botw_profile()
    {
        Directory.CreateDirectory(Path.Combine(root, "Actor", "Pack"));
        File.WriteAllBytes(Path.Combine(root, "Actor", "Pack", "Item_Amiibo_DropTable_000.sbactorpack"), "Yaz0"u8.ToArray());
        File.WriteAllBytes(Path.Combine(root, "Actor", "Pack", "Reward.bdrop"), "AAMP"u8.ToArray());
        var report = RomFsAnalyzer.Analyze(RomFsIndex.Create(root));
        Assert.Equal("BOTW", report.Game?.Code); Assert.Single(report.KeywordMatches["amiibo"]); Assert.Single(report.AampCandidates); Assert.Single(report.SarcCandidates);
    }
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
