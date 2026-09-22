namespace AmiiboRewards.Domain;

public enum TotkSelectorKind { NumberingId, CharacterId, CharacterBaseId, Default }
public sealed record TotkDropEntry(string ActorNameShort, decimal Weight, string? DropGameData, bool IsCheckDropGameData, string? TreasureBoxActorShort);
public sealed record TotkDropPool(string ResultType, int MinDrops, int MaxDrops, IReadOnlyList<TotkDropEntry> Entries);
public sealed record TotkRewardRule(int SourceOrder, TotkSelectorKind SelectorKind, string SelectorValue, string? SpecialDeal, IReadOnlyList<TotkDropPool> Pools);
public sealed record TotkHitRateInfo(decimal? DropNumRate1st, decimal? GreatHitRate1st, decimal? SmallHitRate1st, decimal? HitRateAdjustStart, decimal? HitRateAdjustEnd);
public sealed record TotkAmiiboSettings(IReadOnlyList<TotkRewardRule> Rules, TotkRewardRule DefaultRule, TotkHitRateInfo HitRates);

public static class TotkAmiiboRuleMatcher
{
    private static readonly IReadOnlyDictionary<string, byte[]> CharacterIds = new Dictionary<string, byte[]>(StringComparer.Ordinal)
    {
        ["ToonLink"] = [0x01, 0x00, 0x01], ["Sheik"] = [0x01, 0x01, 0x01]
    };
    private static readonly IReadOnlyDictionary<string, byte[]> CharacterBases = new Dictionary<string, byte[]>(StringComparer.Ordinal)
    {
        ["Link"] = [0x01, 0x00], ["Zelda"] = [0x01, 0x01], ["Ganon"] = [0x01, 0x02], ["WolfLink"] = [0x01, 0x03],
        ["Darukel"] = [0x01, 0x05], ["Uruboza"] = [0x01, 0x06], ["Mifar"] = [0x01, 0x07], ["Reval"] = [0x01, 0x08],
        ["Guardian"] = [0x01, 0x40], ["Bokoblin"] = [0x01, 0x41]
    };

    public static TotkRewardRule Match(TotkAmiiboSettings settings, AmiiboIdentity identity)
    {
        foreach (var rule in settings.Rules.Where(x => x.SelectorKind == TotkSelectorKind.NumberingId).OrderBy(x => x.SourceOrder))
        {
            if (rule.SelectorKind == TotkSelectorKind.NumberingId && ushort.TryParse(rule.SelectorValue, out var id) && id == identity.NumberingId) return rule;
        }
        foreach (var rule in settings.Rules.Where(x => x.SelectorKind == TotkSelectorKind.CharacterId).OrderBy(x => x.SourceOrder))
        {
            if (rule.SelectorKind == TotkSelectorKind.CharacterId && CharacterIds.TryGetValue(rule.SelectorValue, out var character) && identity.CharacterId.AsSpan().SequenceEqual(character)) return rule;
        }
        foreach (var rule in settings.Rules.Where(x => x.SelectorKind == TotkSelectorKind.CharacterBaseId).OrderBy(x => x.SourceOrder))
        {
            if (rule.SelectorKind == TotkSelectorKind.CharacterBaseId && CharacterBases.TryGetValue(rule.SelectorValue, out var @base) && identity.CharacterId.AsSpan(0, 2).SequenceEqual(@base)) return rule;
        }
        return settings.DefaultRule;
    }

    public static decimal NormalizedProbability(decimal weight, IEnumerable<TotkDropEntry> eligible)
    {
        var total = eligible.Sum(x => x.Weight);
        return total <= 0 ? 0 : Math.Round(weight / total * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static bool MatchesSelector(TotkSelectorKind kind, string value, AmiiboCatalogEntry entry) => kind switch
    {
        TotkSelectorKind.NumberingId => ushort.TryParse(value, out var numberingId) && entry.NumberingId == numberingId,
        TotkSelectorKind.CharacterId => CharacterIds.TryGetValue(value, out var characterId) && entry.CharacterId.Equals(AmiiboCatalogImporter.Format(characterId), StringComparison.OrdinalIgnoreCase),
        TotkSelectorKind.CharacterBaseId => CharacterBases.TryGetValue(value, out var characterBaseId) && entry.CharacterBaseId.Equals(AmiiboCatalogImporter.Format(characterBaseId), StringComparison.OrdinalIgnoreCase),
        _ => false
    };

    /// <summary>Human-readable fallback used only when a selector has no concrete local catalog match.</summary>
    public static readonly IReadOnlyDictionary<string, string> FamilyDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Link"] = "Link", ["Zelda"] = "Zelda", ["Ganon"] = "Ganondorf", ["WolfLink"] = "Lobo Link",
        ["Darukel"] = "Daruk", ["Uruboza"] = "Urbosa", ["Mifar"] = "Mipha", ["Reval"] = "Revali",
        ["Guardian"] = "Guardian", ["Bokoblin"] = "Bokoblin", ["ToonLink"] = "Toon Link", ["Sheik"] = "Sheik"
    };
}
