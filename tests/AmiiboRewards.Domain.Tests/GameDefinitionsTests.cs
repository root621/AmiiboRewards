using AmiiboRewards.Domain;

namespace AmiiboRewards.Domain.Tests;

public sealed class GameDefinitionsTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "amiibo-detection-" + Guid.NewGuid());
    private string DirectoryAt(string path) { var target = Path.Combine(root, path); Directory.CreateDirectory(target); return target; }
    private void FileAt(string path) { var file = Path.Combine(root, path); Directory.CreateDirectory(Path.GetDirectoryName(file)!); File.WriteAllBytes(file, []); }

    [Fact]
    public void Detects_both_games_without_using_folder_names()
    {
        FileAt("first/romfs/Actor/Pack/Item_Amiibo_DropTable_000.sbactorpack");
        FileAt("second/romfs/Pack/ZsDic.pack.zs");
        FileAt("second/romfs/Pack/ResidentCommon.pack.zs");
        var scan = GameDefinitions.Scan(root);
        Assert.Equal(new[] { "BOTW", "TOTK" }, scan.Detected.Select(d => d.Game.Code));
        Assert.True(scan.Detected[0].Game.CanImport);
        Assert.True(scan.Detected[1].Game.CanImport);
    }

    [Fact]
    public void Accepts_a_direct_romfs_folder()
    {
        FileAt("Actor/Pack/Item_Amiibo_DropTable_013.sbactorpack");
        Assert.Equal("BOTW", Assert.Single(GameDefinitions.Scan(root).Detected).Game.Code);
    }

    [Fact]
    public void Does_not_guess_from_title_id_or_incomplete_copy()
    {
        FileAt("0100F2C0115B6000/romfs/Pack/ZsDic.pack.zs");
        var scan = GameDefinitions.Scan(root);
        Assert.Empty(scan.Detected);
        Assert.NotEmpty(scan.Warnings);
    }

    [Fact]
    public void Reports_missing_folder_without_throwing()
    {
        var scan = GameDefinitions.Scan(root);
        Assert.Empty(scan.Detected); Assert.Single(scan.Warnings);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
}
