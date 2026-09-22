using AmiiboRewards.Domain;
using AmiiboRewards.Infrastructure;

namespace AmiiboRewards.Domain.Tests;

public sealed class TotkAssetResolverTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "amiibo-rewards-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ResolvesDirectNativeIcon()
    {
        Create("UI/Tex/Icon/Item_Ore_F.bntx.zs");

        var result = await ResolveAsync("Item_Ore_F");

        Assert.Equal("UI/Tex/Icon/Item_Ore_F.bntx.zs", result);
    }

    [Fact]
    public async Task ResolvesFishAndArrowActorAliasesToPlayerFacingIcons()
    {
        Create("UI/Tex/Icon/Item_FishGet_H.bntx.zs");
        Create("UI/Tex/Icon/NormalArrow.bntx.zs");

        Assert.Equal("UI/Tex/Icon/Item_FishGet_H.bntx.zs", await ResolveAsync("Animal_Fish_H"));
        Assert.Equal("UI/Tex/Icon/NormalArrow.bntx.zs", await ResolveAsync("Obj_ArrowNormal_A_01"));
    }

    [Fact]
    public async Task ResolvesColoredEquipmentVariantWhenNoBaseIconExists()
    {
        Create("UI/Tex/Icon/Armor_181_Head_Blue.bntx.zs");

        Assert.Equal("UI/Tex/Icon/Armor_181_Head_Blue.bntx.zs", await ResolveAsync("Armor_181_Head"));
    }

    [Fact]
    public async Task LeavesOutcomesWithoutNativeIconsUnresolved()
    {
        Assert.Null(await ResolveAsync("Barrel"));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    private async Task<string?> ResolveAsync(string sourceId)
    {
        Directory.CreateDirectory(root);
        return await new TotkAssetResolver().ResolveAsync(GameProfiles.Totk, RomFsIndex.Create(root), sourceId);
    }

    private void Create(string relativePath)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, []);
    }
}