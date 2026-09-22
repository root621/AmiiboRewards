using AmiiboRewards.Domain;

namespace AmiiboRewards.Domain.Tests;

public sealed class AmiiboAssociationExpansionTests
{
    private static AmiiboCatalogEntry Entry(string sourceFile, string displayName, string characterId, string characterBaseId, ushort numberingId, byte seriesId = 1, byte nfpType = 0) => new()
    {
        SourceFile = sourceFile,
        DisplayName = displayName,
        CharacterId = characterId,
        CharacterBaseId = characterBaseId,
        SeriesId = seriesId,
        NumberingId = numberingId,
        NfpType = nfpType,
        SourceSha256 = "hash-" + sourceFile
    };

    [Fact]
    public void CharacterBaseId_link_expands_to_multiple_distinct_entries_not_a_joined_name()
    {
        var catalog = new[]
        {
            Entry("/a/link.bin", "Link", "01-00-00", "01-00", 1048),
            Entry("/a/link-totk.bin", "Link (TotK)", "01-00-00", "01-00", 1050),
            Entry("/a/link-ww.bin", "Link (Wind Waker)", "01-00-00", "01-00", 1049),
        };
        var selectors = new List<(int Key, TotkSelectorKind? Kind, string Value)> { (1, TotkSelectorKind.CharacterBaseId, "Link") };

        var result = AmiiboAssociationExpansion.ExpandGroup(selectors, catalog);

        var names = result[1].Select(x => x.DisplayName).OrderBy(x => x).ToList();
        Assert.Equal(3, names.Count);
        Assert.All(names, name => Assert.DoesNotContain(" / ", name));
        Assert.Equal(["Link", "Link (TotK)", "Link (Wind Waker)"], names);
    }

    [Fact]
    public void CharacterBaseId_zelda_expands_to_multiple_distinct_entries()
    {
        var catalog = new[]
        {
            Entry("/a/zelda.bin", "Zelda", "01-01-00", "01-01", 200),
            Entry("/a/zelda-totk.bin", "Zelda (TotK)", "01-01-00", "01-01", 201),
            Entry("/a/zelda-ww.bin", "Zelda (Wind Waker)", "01-01-00", "01-01", 202),
        };
        var selectors = new List<(int Key, TotkSelectorKind? Kind, string Value)> { (1, TotkSelectorKind.CharacterBaseId, "Zelda") };

        var result = AmiiboAssociationExpansion.ExpandGroup(selectors, catalog);

        Assert.Equal(3, result[1].Count);
    }

    [Fact]
    public void Duplicate_bin_dumps_of_the_same_identity_collapse_to_one_entry()
    {
        var catalog = new[]
        {
            Entry("/a/link (1).bin", "Link", "01-00-00", "01-00", 1048),
            Entry("/backup/link-copy.bin", "Link", "01-00-00", "01-00", 1048),
        };

        var deduplicated = AmiiboAssociationExpansion.Deduplicate(catalog);

        Assert.Single(deduplicated);
    }

    [Fact]
    public void Exact_numbering_id_selector_excludes_that_amiibo_from_sibling_family_selector()
    {
        var catalog = new[]
        {
            Entry("/a/link.bin", "Link", "01-00-00", "01-00", 1048),
            Entry("/a/link-totk.bin", "Link (TotK)", "01-00-00", "01-00", 1050),
        };
        var selectors = new List<(int Key, TotkSelectorKind? Kind, string Value)>
        {
            (1, TotkSelectorKind.NumberingId, "1048"),
            (2, TotkSelectorKind.CharacterBaseId, "Link"),
        };

        var result = AmiiboAssociationExpansion.ExpandGroup(selectors, catalog);

        Assert.Equal("Link", Assert.Single(result[1]).DisplayName);
        var familyMatches = result[2].Select(x => x.DisplayName).ToList();
        Assert.DoesNotContain("Link", familyMatches);
        Assert.Contains("Link (TotK)", familyMatches);
    }

    [Fact]
    public void Display_names_preserve_local_qualifiers()
    {
        var catalog = new[] { Entry("/a/link-totk.bin", "Link (TotK)", "01-00-00", "01-00", 1050) };
        var selectors = new List<(int Key, TotkSelectorKind? Kind, string Value)> { (1, TotkSelectorKind.CharacterBaseId, "Link") };

        var result = AmiiboAssociationExpansion.ExpandGroup(selectors, catalog);

        Assert.Equal("Link (TotK)", Assert.Single(result[1]).DisplayName);
    }

    [Fact]
    public void Parses_selector_prefixes()
    {
        Assert.Equal((TotkSelectorKind.NumberingId, "1048"), AmiiboAssociationExpansion.ParseSelector("NumberingID_1048"));
        Assert.Equal((TotkSelectorKind.CharacterId, "ToonLink"), AmiiboAssociationExpansion.ParseSelector("CharacterID_ToonLink"));
        Assert.Equal((TotkSelectorKind.CharacterBaseId, "Link"), AmiiboAssociationExpansion.ParseSelector("CharacterBaseID_Link"));
        Assert.Equal((TotkSelectorKind.Default, "Default"), AmiiboAssociationExpansion.ParseSelector("Default"));
    }

    [Fact]
    public void Numbering_id_selector_excludes_that_amiibo_from_sibling_character_id_selector()
    {
        // Toon Link (Smash series) and Link (Wind Waker, Zelda 30th anniversary) legitimately share the
        // same in-game CharacterId, but an exact NumberingId rule for one must not also surface it here.
        var catalog = new[]
        {
            Entry("/a/toon-link.bin", "Toon Link", "01-00-01", "01-00", 22),
            Entry("/a/link-ww.bin", "Link (Wind Waker)", "01-00-01", "01-00", 848),
        };
        var selectors = new List<(int Key, TotkSelectorKind? Kind, string Value)>
        {
            (1, TotkSelectorKind.NumberingId, "848"),
            (2, TotkSelectorKind.CharacterId, "ToonLink"),
        };

        var result = AmiiboAssociationExpansion.ExpandGroup(selectors, catalog);

        Assert.Equal("Link (Wind Waker)", Assert.Single(result[1]).DisplayName);
        Assert.Equal("Toon Link", Assert.Single(result[2]).DisplayName);
    }
}
