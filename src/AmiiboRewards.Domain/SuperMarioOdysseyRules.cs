namespace AmiiboRewards.Domain;

/// <summary>
/// Super Mario Odyssey amiibo rules encode a family selector and, optionally, a specific numbering ID.
/// A rule is considered matched when both configured values match the identity, if present.
/// </summary>
public sealed record OdysseyAmiiboRule(ushort CharacterId, int? NumberingId, string ItemName)
{
    public bool Matches(AmiiboIdentity identity) => SuperMarioOdysseyAmiiboRuleMatcher.Matches(this, identity);
}

public sealed record OdysseyCostumeItem(string ItemName, string ComponentType, int? CostumeId, string SourceId);

public sealed record OdysseyProgressionRoute(string ItemType, int? MoonNum, string? ClearWorld, string? CoinType, int Price);

public sealed record OdysseyCostumeMetadata(IReadOnlyList<string> Components, IReadOnlyList<OdysseyProgressionRoute> NormalRoutes);

public sealed record OdysseyItemListEntry(string ItemName, string ItemType, string? StoreName, int Price, string? MoonName, int MoonNum, string? Description)
{
    public string? StoreName { get; init; } = StoreName;
    public int MoonNum { get; init; } = MoonNum;
    public string? MoonName { get; init; } = MoonName;
}

public sealed record OdysseyCostumeBundle(
    string ItemName,
    IReadOnlyList<OdysseyCostumeItem> Components,
    int? MoonNum = null,
    string? StoreName = null,
    string? ItemType = null)
{
    public string ItemName { get; init; } = ItemName;
    public IReadOnlyList<OdysseyCostumeItem> Components { get; init; } = Components;
    public int? MoonNum { get; init; } = MoonNum;
    public string? StoreName { get; init; } = StoreName;
    public string? ItemType { get; init; } = ItemType;
}

public static class SuperMarioOdysseyAmiiboRuleMatcher
{
    public static bool Matches(OdysseyAmiiboRule rule, AmiiboIdentity identity)
    {
        if (identity.CharacterIdAsUInt16 != rule.CharacterId) return false;
        if (rule.NumberingId is int numberingId && identity.NumberingId != numberingId) return false;
        return true;
    }

    public static IEnumerable<OdysseyCostumeBundle> NormalizeCostumes(
        IEnumerable<OdysseyCostumeItem> source,
        IReadOnlyDictionary<string, OdysseyItemListEntry>? itemList = null)
    {
        foreach (var group in source.GroupBy(x => x.ItemName, StringComparer.OrdinalIgnoreCase))
        {
            var components = group.OrderBy(x => x.ComponentType, StringComparer.OrdinalIgnoreCase).ToList();
            var itemName = group.Key;
            var metadata = itemList is not null && itemList.TryGetValue(itemName, out var entry) ? entry : null;
            var storeName = metadata?.MoonName ?? metadata?.StoreName;
            yield return new OdysseyCostumeBundle(
                itemName,
                components,
                metadata?.MoonNum,
                storeName,
                metadata?.ItemType);
        }
    }
}
