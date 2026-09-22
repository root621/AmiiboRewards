using AmiiboRewards.Domain;

namespace AmiiboRewards.Domain.Tests;

public sealed class TotkAmiiboRulesTests
{
    private static TotkRewardRule Rule(int order, TotkSelectorKind kind, string value, params TotkDropPool[] pools) => new(order, kind, value, null, pools);
    private static TotkAmiiboSettings Settings(params TotkRewardRule[] rules) => new(rules, Rule(99, TotkSelectorKind.Default, "Default"), new(null, null, null, null, null));
    private static AmiiboIdentity Id(byte a, byte b, byte c, ushort number = 0) => new([a, b, c], 0, number, 0, 0);
    [Fact] public void Reads_ntags215_identity() { var dump = new byte[0x5c]; new byte[] { 1, 0, 0, 0, 4, 0x18, 9, 2 }.CopyTo(dump, 0x54); var id = AmiiboIdentity.Read(dump); Assert.Equal((ushort)1048, id.NumberingId); Assert.Equal(new byte[] { 1, 0, 0 }, id.CharacterId); Assert.Equal(new byte[] { 1, 0 }, id.CharacterBaseId); Assert.Equal((byte)9, id.NfpType); Assert.Equal((byte)2, id.Version); }
    [Fact] public void Exact_numbering_beats_generic_link() => Assert.Equal("1048", TotkAmiiboRuleMatcher.Match(Settings(Rule(0, TotkSelectorKind.NumberingId, "1048"), Rule(1, TotkSelectorKind.CharacterBaseId, "Link")), Id(1, 0, 0, 1048)).SelectorValue);
    [Fact] public void Toon_and_sheik_use_full_character_id() { var settings = Settings(Rule(0, TotkSelectorKind.CharacterId, "ToonLink"), Rule(1, TotkSelectorKind.CharacterId, "Sheik")); Assert.Equal("ToonLink", TotkAmiiboRuleMatcher.Match(settings, Id(1, 0, 1)).SelectorValue); Assert.Equal("Sheik", TotkAmiiboRuleMatcher.Match(settings, Id(1, 1, 1)).SelectorValue); }
    [Fact] public void Unknown_link_uses_base_and_unknown_uses_default() { var settings = Settings(Rule(0, TotkSelectorKind.CharacterBaseId, "Link")); Assert.Equal("Link", TotkAmiiboRuleMatcher.Match(settings, Id(1, 0, 9)).SelectorValue); Assert.Equal("Default", TotkAmiiboRuleMatcher.Match(settings, Id(9, 9, 9)).SelectorValue); }
    [Fact] public void Normalizes_weights_instead_of_copying_them_as_percentage() { var pool = new[] { new TotkDropEntry("A", 60, null, false, null), new TotkDropEntry("B", 40, null, false, null) }; Assert.Equal(60m, TotkAmiiboRuleMatcher.NormalizedProbability(pool[0].Weight, pool)); }
    [Fact] public void Empty_pool_is_safe() => Assert.Equal(0m, TotkAmiiboRuleMatcher.NormalizedProbability(1, []));
    [Fact] public void Numbering_id_precedes_character_rules_even_when_source_order_is_later() => Assert.Equal("1048", TotkAmiiboRuleMatcher.Match(Settings(Rule(0, TotkSelectorKind.CharacterBaseId, "Link"), Rule(1, TotkSelectorKind.NumberingId, "1048")), Id(1, 0, 0, 1048)).SelectorValue);
}
