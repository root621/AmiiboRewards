using AmiiboRewards.Domain;

namespace AmiiboRewards.Application;

public sealed record RewardSearchResult(string InternalId, string Name, string? Description, RewardType RewardType, string? IconPath, IReadOnlyList<AmiiboRewardResult> Amiibo);
public sealed record AmiiboRewardResult(string Name, AmiiboMappingStatus MappingStatus, string Pool, decimal Probability, decimal? RawWeight, int? MinCount, int? MaxCount, string? Condition, InteractionKind InteractionKind, OutcomeKind OutcomeKind, string? SpecialMetadata);
public interface IRewardSearchService { Task<IReadOnlyList<RewardSearchResult>> SearchAsync(string query, string locale, CancellationToken cancellationToken, string game = "BOTW"); }
