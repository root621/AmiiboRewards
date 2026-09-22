using AmiiboRewards.Domain;

namespace AmiiboRewards.Tools.Botw;

/// <summary>Only TOTK's AmiiboSetting schema lives here; archive/compression/BYML live in the common engine.</summary>
internal sealed class TotkAmiiboInteractionAdapter : IGameInteractionAdapter
{
    public string Id => GameProfiles.Totk.AdapterId;
    public bool Supports(GameProfile profile, RomFsIndex index) => profile.AdapterId == Id && profile.RequiredRomFsMarkers.All(marker => index.Files.Any(x => x.RelativePath.Equals(marker, StringComparison.OrdinalIgnoreCase)));

    public async Task<IReadOnlyList<GameInteraction>> ReadAsync(GameProfile profile, RomFsIndex index, CancellationToken cancellationToken = default)
    {
        if (!Supports(profile, index)) throw new InvalidDataException("The RomFS does not contain TOTK's required amiibo files.");
        var source = await TotkRomFsReader.ReadAsync(profile, index, cancellationToken); var output = new List<GameInteraction>();
        foreach (var rule in source.Settings.Rules.Append(source.Settings.DefaultRule)) AddRule(output, rule, source.SourceSha256);
        return output;
    }

    private static void AddRule(List<GameInteraction> output, TotkRewardRule rule, string hash)
    {
        var selector = rule.SelectorKind switch { TotkSelectorKind.NumberingId => $"NumberingID_{rule.SelectorValue}", TotkSelectorKind.CharacterId => $"CharacterID_{rule.SelectorValue}", TotkSelectorKind.CharacterBaseId => $"CharacterBaseID_{rule.SelectorValue}", _ => "Default" };
        var index = 0;
        foreach (var pool in rule.Pools)
        foreach (var entry in pool.Entries)
        {
            var conditional = entry.IsCheckDropGameData && !string.IsNullOrWhiteSpace(entry.DropGameData) ? entry.DropGameData : null;
            var kind = Classify(entry.ActorNameShort);
            output.Add(new GameInteraction(selector, InteractionKind.Drop, kind, entry.ActorNameShort, pool.ResultType,
                entry.Weight, TotkAmiiboRuleMatcher.NormalizedProbability(entry.Weight, pool.Entries), pool.MinDrops, pool.MaxDrops,
                conditional, string.IsNullOrEmpty(entry.TreasureBoxActorShort) ? null : entry.TreasureBoxActorShort,
                $"{TotkRomFsReader.ResidentArchive}::{TotkRomFsReader.SettingsPath}", hash, index++));
        }
        if (!string.IsNullOrWhiteSpace(rule.SpecialDeal) && !rule.SpecialDeal.Equals("None", StringComparison.OrdinalIgnoreCase))
            output.Add(new GameInteraction(selector, InteractionKind.Special, OutcomeKind.Companion, $"Special:{rule.SpecialDeal}", "Special", null, null, null, null, null,
                rule.SpecialDeal, $"{TotkRomFsReader.ResidentArchive}::{TotkRomFsReader.SettingsPath}", hash, index));
    }
    private static OutcomeKind Classify(string actor) => actor is "Barrel" or "BarrelBomb" or "TimerBarrelBomb" or "Kibako_Contain_01" or "Obj_BreakBoxIron" ? OutcomeKind.Container : actor.StartsWith("Obj_", StringComparison.Ordinal) || actor.StartsWith("Animal_", StringComparison.Ordinal) ? OutcomeKind.ActorSpawn : OutcomeKind.Item;
}
