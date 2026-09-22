using AmiiboRewards.Domain;

namespace AmiiboRewards.Domain.Tests;

public sealed class SuperMarioOdysseyTests
{
    [Fact]
    public void Detects_odyssey_from_item_list_archive()
    {
        var root = Path.Combine(Path.GetTempPath(), "amiibo-odyssey-detection-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "SystemData"));
        File.WriteAllBytes(Path.Combine(root, "SystemData", "ItemList.szs"), [0x53, 0x41, 0x52, 0x43]);

        try
        {
            var scan = GameDefinitions.Scan(root);
            var found = Assert.Single(scan.Detected);
            Assert.Equal("ODYSSEY", found.Game.Code);
            Assert.True(found.Game.CanImport);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Converts_family_id_and_numbering_id_from_amiibo_identity()
    {
        var dump = new byte[0x5c];
        new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x34, 0x01, 0x02 }.CopyTo(dump, 0x54);
        var identity = AmiiboIdentity.Read(dump);
        Assert.Equal((ushort)0, identity.CharacterIdAsUInt16);
        Assert.Equal((ushort)52, identity.NumberingId);

        var luigi = new byte[0x5c];
        new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x35, 0x01, 0x02 }.CopyTo(luigi, 0x54);
        Assert.Equal((ushort)256, AmiiboIdentity.Read(luigi).CharacterIdAsUInt16);

        var peach = new byte[0x5c];
        new byte[] { 0x00, 0x02, 0x00, 0x00, 0x00, 0x36, 0x01, 0x02 }.CopyTo(peach, 0x54);
        Assert.Equal((ushort)512, AmiiboIdentity.Read(peach).CharacterIdAsUInt16);

        var bowser = new byte[0x5c];
        new byte[] { 0x00, 0x05, 0x00, 0x00, 0x00, 0x39, 0x01, 0x02 }.CopyTo(bowser, 0x54);
        Assert.Equal((ushort)1280, AmiiboIdentity.Read(bowser).CharacterIdAsUInt16);
    }

    [Fact]
    public void Character_only_rule_matches_a_family()
    {
        var family = new OdysseyAmiiboRule(256, null, "MarioColorLuigi");
        var mario = new byte[0x5c];
        new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x35, 0x01, 0x02 }.CopyTo(mario, 0x54);
        Assert.True(SuperMarioOdysseyAmiiboRuleMatcher.Matches(family, AmiiboIdentity.Read(mario)));
    }

    [Fact]
    public void Character_and_numbering_rule_requires_both_values()
    {
        var rule = new OdysseyAmiiboRule(0, 881, "MarioTuxedo");
        var normalMario = new byte[0x5c];
        new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x34, 0x01, 0x02 }.CopyTo(normalMario, 0x54);
        var weddingMario = new byte[0x5c];
        new byte[] { 0x00, 0x00, 0x00, 0x00, 0x03, 0x71, 0x01, 0x02 }.CopyTo(weddingMario, 0x54);

        Assert.False(SuperMarioOdysseyAmiiboRuleMatcher.Matches(rule, AmiiboIdentity.Read(normalMario)));
        Assert.True(SuperMarioOdysseyAmiiboRuleMatcher.Matches(rule, AmiiboIdentity.Read(weddingMario)));
    }

    [Fact]
    public void Wedding_rules_match_the_right_amiibos()
    {
        var marioWedding = new OdysseyAmiiboRule(0, 881, "MarioTuxedo");
        var peachWedding = new OdysseyAmiiboRule(512, 882, "MarioPeach");
        var bowserWedding = new OdysseyAmiiboRule(1280, 883, "MarioKoopa");

        var marioWeddingDump = new byte[0x5c];
        new byte[] { 0x00, 0x00, 0x00, 0x00, 0x03, 0x71, 0x01, 0x02 }.CopyTo(marioWeddingDump, 0x54);
        var peachWeddingDump = new byte[0x5c];
        new byte[] { 0x00, 0x02, 0x00, 0x00, 0x03, 0x72, 0x01, 0x02 }.CopyTo(peachWeddingDump, 0x54);
        var bowserWeddingDump = new byte[0x5c];
        new byte[] { 0x00, 0x05, 0x00, 0x00, 0x03, 0x73, 0x01, 0x02 }.CopyTo(bowserWeddingDump, 0x54);

        Assert.True(SuperMarioOdysseyAmiiboRuleMatcher.Matches(marioWedding, AmiiboIdentity.Read(marioWeddingDump)));
        Assert.True(SuperMarioOdysseyAmiiboRuleMatcher.Matches(peachWedding, AmiiboIdentity.Read(peachWeddingDump)));
        Assert.True(SuperMarioOdysseyAmiiboRuleMatcher.Matches(bowserWedding, AmiiboIdentity.Read(bowserWeddingDump)));
    }

    [Fact]
    public void Luigi_family_resolves_to_mario_color_luigi()
    {
        var rule = new OdysseyAmiiboRule(256, null, "MarioColorLuigi");
        var luigi = new byte[0x5c];
        new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x35, 0x01, 0x02 }.CopyTo(luigi, 0x54);
        Assert.Equal("MarioColorLuigi", rule.Matches(AmiiboIdentity.Read(luigi)) ? rule.ItemName : "");
    }

    [Fact]
    public void Cap_and_clothes_with_the_same_item_name_are_normalized_once()
    {
        var source = new[]
        {
            new OdysseyCostumeItem("MarioColorLuigi", "Cap", 160, "ItemCap"),
            new OdysseyCostumeItem("MarioColorLuigi", "Clothes", 180, "ItemCloth")
        };

        var normalized = SuperMarioOdysseyAmiiboRuleMatcher.NormalizeCostumes(source).ToList();
        var single = Assert.Single(normalized);
        Assert.Equal("MarioColorLuigi", single.ItemName);
        Assert.Contains("Cap", single.Components.Select(x => x.ComponentType));
        Assert.Contains("Clothes", single.Components.Select(x => x.ComponentType));
    }

    [Fact]
    public void Item_list_moon_metadata_is_associated_with_the_correct_costume()
    {
        var costumes = new[]
        {
            new OdysseyCostumeItem("MarioDoctor", "Cap", 220, "ItemCap"),
            new OdysseyCostumeItem("MarioDoctor", "Clothes", 240, "ItemCloth")
        };

        var itemList = new Dictionary<string, OdysseyItemListEntry>
        {
            ["MarioDoctor"] = new("MarioDoctor", "Costume", "Moon", 240, "MoonRock", 220, "Amiibo unlock")
        };

        var normalized = SuperMarioOdysseyAmiiboRuleMatcher.NormalizeCostumes(costumes, itemList).ToList();
        var single = Assert.Single(normalized);
        Assert.Equal(220, single.MoonNum);
        Assert.Equal("MoonRock", single.StoreName);
    }
}
