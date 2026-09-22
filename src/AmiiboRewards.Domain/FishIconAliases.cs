namespace AmiiboRewards.Domain;

public static class FishIconAliases
{
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Animal_Fish_A"] = "Item_FishGet_A", ["Animal_Fish_B"] = "Item_FishGet_B",
        ["Animal_Fish_C"] = "Item_FishGet_C", ["Animal_Fish_D"] = "Item_FishGet_D",
        ["Animal_Fish_E"] = "Item_FishGet_E", ["Animal_Fish_F"] = "Item_FishGet_F",
        ["Animal_Fish_G"] = "Item_FishGet_G", ["Animal_Fish_H"] = "Item_FishGet_H",
        ["Animal_Fish_I"] = "Item_FishGet_I", ["Animal_Fish_J"] = "Item_FishGet_J",
        ["Animal_Fish_L"] = "Item_FishGet_L", ["Animal_Fish_X"] = "Item_FishGet_X"
    };

    public static string Resolve(string internalId) => Aliases.TryGetValue(internalId, out var alias) ? alias : internalId;
}
