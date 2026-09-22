using AmiiboRewards.Application;
using Microsoft.EntityFrameworkCore;

namespace AmiiboRewards.Infrastructure;

public sealed class EfRewardSearchService(AmiiboRewardsDbContext db) : IRewardSearchService
{
    public async Task<IReadOnlyList<RewardSearchResult>> SearchAsync(string query, string locale, CancellationToken cancellationToken, string game = "BOTW")
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var original = query.Trim();
        var translated = ExpandSpanishTerm(original);
        var needle = $"%{original}%";
        var translatedNeedle = $"%{translated}%";
        locale = string.IsNullOrWhiteSpace(locale) ? "USes" : locale;
        return await db.Rewards.AsNoTracking().Where(r => r.Game!.Code == game.ToUpperInvariant()).Where(r =>
                r.Localizations.Any(l => l.Locale == locale && (EF.Functions.ILike(l.Name, needle) || EF.Functions.ILike(l.Name, translatedNeedle))) ||
                EF.Functions.ILike(r.Name, needle) ||
                EF.Functions.ILike(r.InternalId, needle) ||
                EF.Functions.ILike(r.Name, translatedNeedle) ||
                EF.Functions.ILike(r.InternalId, translatedNeedle))
            .OrderBy(r => r.Localizations.Where(l => l.Locale == locale).Select(l => l.Name).FirstOrDefault() ?? r.Name).Take(50).Select(r => new RewardSearchResult(r.InternalId, r.Localizations.Where(l => l.Locale == locale).Select(l => l.Name).FirstOrDefault() ?? r.Name, r.Localizations.Where(l => l.Locale == locale).Select(l => l.Description).FirstOrDefault() ?? r.Description, r.RewardType, r.IconAsset == null ? null : r.IconAsset.OutputPath, r.AmiiboRewards.Select(ar => new AmiiboRewardResult(ar.Amiibo!.Name ?? ar.Amiibo.InternalTableId, ar.Amiibo!.MappingStatus, ar.Pool, ar.Probability, ar.RawWeight, ar.MinCount, ar.MaxCount, ar.Condition, ar.InteractionKind, ar.OutcomeKind, ar.SpecialMetadata)).ToList())).ToListAsync(cancellationToken);
    }

    private static string ExpandSpanishTerm(string query) => query.ToLowerInvariant() switch
    {
        "arco" => "Bow",
        "carne" => "Meat",
        "flecha" or "flechas" => "Arrow",
        "pescado" or "pez" => "Fish",
        _ => query
    };
}
