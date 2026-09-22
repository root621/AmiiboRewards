using AmiiboRewards.Domain;

namespace AmiiboRewards.Infrastructure;

public sealed class TotkAssetResolver : IGameAssetResolver
{
    public Task<string?> ResolveAsync(GameProfile profile, RomFsIndex index, string sourceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var assetIds = new[] { FishIconAliases.Resolve(sourceId), sourceId == "Obj_ArrowNormal_A_01" ? "NormalArrow" : sourceId }
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var candidates = assetIds.SelectMany(assetId => new[]
        {
            $"UI/Tex/Icon/{assetId}.bntx.zs",
            $"UI/Tex/Icon/{assetId}.bntx",
            $"UI/Tex/PictureBook/{assetId}_Icon.bntx.zs",
            $"UI/Tex/PictureBook/{assetId}_Icon.bntx"
        });
        var match = candidates.FirstOrDefault(candidate => index.Files.Any(file => file.RelativePath.Equals(candidate, StringComparison.OrdinalIgnoreCase)));
        if (match is null)
        {
            match = assetIds.SelectMany(assetId => index.Files
                    .Where(file => file.RelativePath.StartsWith($"UI/Tex/Icon/{assetId}_", StringComparison.OrdinalIgnoreCase))
                    .Where(file => file.RelativePath.EndsWith(".bntx.zs", StringComparison.OrdinalIgnoreCase) || file.RelativePath.EndsWith(".bntx", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .Select(file => file.RelativePath))
                .FirstOrDefault();
        }
        return Task.FromResult(match);
    }
}