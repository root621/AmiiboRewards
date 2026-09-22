namespace AmiiboRewards.Domain;

public sealed class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Version { get; set; }
    public string? RomFsPath { get; set; }
    public DateTimeOffset? LastDetectedAt { get; set; }
    public List<Amiibo> Amiibo { get; } = [];
    public List<Reward> Rewards { get; } = [];
    public List<ImportRun> ImportRuns { get; } = [];
}

public sealed class AppSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Key { get; set; }
    public required string Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Amiibo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string InternalTableId { get; set; }
    public string? Name { get; set; }
    public AmiiboMappingStatus MappingStatus { get; set; } = AmiiboMappingStatus.Unresolved;
    public string? MappingSource { get; set; }
    public Game? Game { get; set; }
    public List<AmiiboReward> Rewards { get; } = [];
}

public sealed class AmiiboCatalogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string SourceFile { get; set; }
    public required string DisplayName { get; set; }
    public required string CharacterId { get; set; }
    public required string CharacterBaseId { get; set; }
    public byte SeriesId { get; set; }
    public ushort NumberingId { get; set; }
    public byte NfpType { get; set; }
    public byte Version { get; set; }
    public required string SourceSha256 { get; set; }
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum AmiiboMappingStatus { Unresolved, Confirmed, CommonPool, Reserved }

public enum RewardType { Item, ActorSpawn, Container, SecondaryDropTable, CostumeUnlock, CharacterUnlock, ModeUnlock, Event, Companion, Special }

public sealed class Reward
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string InternalId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public RewardType RewardType { get; set; }
    public Guid? IconAssetId { get; set; }
    public string? NameSourcePath { get; set; }
    public string? DescriptionSourcePath { get; set; }
    public string? NameSourceSha256 { get; set; }
    public string? DescriptionSourceSha256 { get; set; }
    public string? Metadata { get; set; }
    public Game? Game { get; set; }
    public Asset? IconAsset { get; set; }
    public List<AmiiboReward> AmiiboRewards { get; } = [];
    public List<RewardLocalization> Localizations { get; } = [];
}

public sealed class RewardLocalization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RewardId { get; set; }
    public required string Locale { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string SourcePath { get; set; }
    public required string SourceSha256 { get; set; }
    public Reward? Reward { get; set; }
}

public sealed class AmiiboReward
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AmiiboId { get; set; }
    public Guid RewardId { get; set; }
    public required string Pool { get; set; }
    public decimal Probability { get; set; }
    public string? Condition { get; set; }
    public required string DropSourcePath { get; set; }
    public required string DropSourceSha256 { get; set; }
    public int EntryIndex { get; set; }
    public decimal? RawWeight { get; set; }
    public int? MinCount { get; set; }
    public int? MaxCount { get; set; }
    public string? SpecialMetadata { get; set; }
    public InteractionKind InteractionKind { get; set; } = InteractionKind.Drop;
    public OutcomeKind OutcomeKind { get; set; } = OutcomeKind.Item;
    public Amiibo? Amiibo { get; set; }
    public Reward? Reward { get; set; }
}

public enum ImportStatus { Running, Succeeded, Failed, SucceededWithWarnings }
public sealed class ImportRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string Kind { get; set; }
    public required string SourcePath { get; set; }
    public required string SourceSha256 { get; set; }
    public ImportStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Summary { get; set; }
    public Game? Game { get; set; }
}

public enum AssetType { Icon }

public sealed class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string InternalId { get; set; }
    public AssetType AssetType { get; set; }
    public required string SourcePath { get; set; }
    public required string OutputPath { get; set; }
    public required string SourceSha256 { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Game? Game { get; set; }
}
