namespace AmiiboRewards.Domain;

public static class TotkAmiiboSettingsReader
{
    public static TotkAmiiboSettings Read(ReadOnlySpan<byte> byml)
    {
        var root = BymlReader.Read(byml);
        var order = 0;
        var rules = List(root, "AmiiboDropActorSettingSet").Select(entry => ReadRule(Map(entry), order++)).ToList();
        var fallback = ReadFallback(root, order);
        return new TotkAmiiboSettings(rules, fallback, ReadHitRates(Map(root.GetValueOrDefault("HitRateInfo"))));
    }

    private static TotkRewardRule ReadRule(IReadOnlyDictionary<string, object?> source, int order)
    {
        var numbering = Text(source, "NumberingID");
        if (!string.IsNullOrWhiteSpace(numbering) && !numbering.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return new(order, TotkSelectorKind.NumberingId, numbering.Replace("NumberingID_", "", StringComparison.Ordinal), Text(source, "AmiiboSpecialDeal"), Pools(source));
        var type = Text(source, "AmiiboSetType");
        if (type == "CharacterID") return new(order, TotkSelectorKind.CharacterId, Text(source, "CharacterIDType") ?? "Unknown", Text(source, "AmiiboSpecialDeal"), Pools(source));
        if (type == "CharacterBaseID") return new(order, TotkSelectorKind.CharacterBaseId, Text(source, "CharacterBaseIDType") ?? "Unknown", Text(source, "AmiiboSpecialDeal"), Pools(source));
        // Future/unknown selector types are still imported as non-matching source rules.
        return new(order, TotkSelectorKind.Default, type ?? "Unknown", Text(source, "AmiiboSpecialDeal"), Pools(source));
    }

    private static TotkRewardRule ReadFallback(IReadOnlyDictionary<string, object?> root, int order) =>
        new(order, TotkSelectorKind.Default, "Default", null, Pools(root, "DefaultDropActorInfoList"));

    private static IReadOnlyList<TotkDropPool> Pools(IReadOnlyDictionary<string, object?> source, string key = "DropActorInfoListSet") => List(source, key).Select(pool =>
    {
        var map = Map(pool); var entries = List(map, "DropActorInfo").Select(entry =>
        {
            var item = Map(entry);
            return new TotkDropEntry(Text(item, "ActorNameShort") ?? "", Decimal(item, "Probability"), Text(item, "DropGameData"), Bool(item, "IsCheclDropGameData"), Text(item, "TreasureBoxActorShort"));
        }).Where(x => x.ActorNameShort.Length > 0).ToList();
        return new TotkDropPool(Text(map, "DropRsultType") ?? "Normal", Integer(map, "MinNumberOfDropChance"), Integer(map, "MaxNumberOfDropChance"), entries);
    }).ToList();

    private static TotkHitRateInfo ReadHitRates(IReadOnlyDictionary<string, object?> source) => new(DecimalNullable(source, "DropNumRate1st"), DecimalNullable(source, "GreatHitRate1st"), DecimalNullable(source, "SmallHitRate1st"), DecimalNullable(source, "HitRateAdjustStart"), DecimalNullable(source, "HitRateAdjustEnd"));
    private static IReadOnlyList<object?> List(IReadOnlyDictionary<string, object?> map, string key) => map.TryGetValue(key, out var value) && value is IReadOnlyList<object?> list ? list : [];
    private static IReadOnlyDictionary<string, object?> Map(object? value) => value as IReadOnlyDictionary<string, object?> ?? new Dictionary<string, object?>();
    private static string? Text(IReadOnlyDictionary<string, object?> map, string key) => map.GetValueOrDefault(key)?.ToString();
    private static bool Bool(IReadOnlyDictionary<string, object?> map, string key) => map.GetValueOrDefault(key) is true || bool.TryParse(Text(map, key), out var result) && result;
    private static int Integer(IReadOnlyDictionary<string, object?> map, string key) => Convert.ToInt32(map.GetValueOrDefault(key) ?? 0);
    private static decimal Decimal(IReadOnlyDictionary<string, object?> map, string key) => Convert.ToDecimal(map.GetValueOrDefault(key) ?? 0m);
    private static decimal? DecimalNullable(IReadOnlyDictionary<string, object?> map, string key) => map.ContainsKey(key) ? Decimal(map, key) : null;
}
