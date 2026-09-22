namespace AmiiboRewards.Domain;

/// <summary>
/// Expands a TOTK selector (NumberingId/CharacterId/CharacterBaseId/Default) into the concrete local
/// amiibo identities it resolves to, so each concrete amiibo can be presented as its own association
/// instead of a single slash-joined display name.
/// </summary>
public static class AmiiboAssociationExpansion
{
    public static (TotkSelectorKind? Kind, string Value) ParseSelector(string internalTableId) => internalTableId switch
    {
        _ when internalTableId.StartsWith("NumberingID_", StringComparison.Ordinal) => (TotkSelectorKind.NumberingId, internalTableId[12..]),
        _ when internalTableId.StartsWith("CharacterID_", StringComparison.Ordinal) => (TotkSelectorKind.CharacterId, internalTableId[12..]),
        _ when internalTableId.StartsWith("CharacterBaseID_", StringComparison.Ordinal) => (TotkSelectorKind.CharacterBaseId, internalTableId[16..]),
        "Default" => (TotkSelectorKind.Default, "Default"),
        _ => (null, internalTableId)
    };

    /// <summary>Stable identity key for deduplicating catalog entries. Never uses the source file path.</summary>
    public static (ushort NumberingId, string CharacterId, byte SeriesId, byte NfpType) IdentityKey(AmiiboCatalogEntry entry) =>
        (entry.NumberingId, entry.CharacterId, entry.SeriesId, entry.NfpType);

    /// <summary>
    /// Collapses multiple .bin dumps of the same physical amiibo (duplicate files, alternate names,
    /// nested folders) into a single catalog identity, keeping the alphabetically-first source for provenance.
    /// </summary>
    public static IReadOnlyList<AmiiboCatalogEntry> Deduplicate(IEnumerable<AmiiboCatalogEntry> entries) =>
        entries
            .GroupBy(IdentityKey)
            .Select(group => group.OrderBy(x => x.SourceFile, StringComparer.OrdinalIgnoreCase).First())
            .ToList();

    /// <summary>
    /// Expands every selector belonging to the same reward into its concrete catalog matches.
    /// Selectors are resolved in precedence order (NumberingId, then CharacterId, then CharacterBaseId)
    /// and each tier excludes any concrete amiibo already claimed by a more specific sibling selector for
    /// that reward, so an exact-match amiibo never also shows up through a more generic rule.
    /// </summary>
    public static IReadOnlyDictionary<TKey, IReadOnlyList<AmiiboCatalogEntry>> ExpandGroup<TKey>(
        IReadOnlyList<(TKey Key, TotkSelectorKind? Kind, string Value)> selectors,
        IReadOnlyList<AmiiboCatalogEntry> catalog) where TKey : notnull
    {
        var claimed = new HashSet<(ushort, string, byte, byte)>();
        var result = new Dictionary<TKey, IReadOnlyList<AmiiboCatalogEntry>>();

        foreach (var kind in new[] { TotkSelectorKind.NumberingId, TotkSelectorKind.CharacterId, TotkSelectorKind.CharacterBaseId })
        {
            foreach (var (key, selectorKind, value) in selectors.Where(s => s.Kind == kind))
            {
                var matches = catalog
                    .Where(entry => TotkAmiiboRuleMatcher.MatchesSelector(selectorKind!.Value, value, entry) && !claimed.Contains(IdentityKey(entry)))
                    .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foreach (var match in matches) claimed.Add(IdentityKey(match));
                result[key] = matches;
            }
        }

        foreach (var (key, kind, _) in selectors.Where(s => s.Kind is null or TotkSelectorKind.Default))
            result[key] = [];

        return result;
    }
}
