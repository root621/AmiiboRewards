using AmiiboRewards.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmiiboRewards.Infrastructure;

public sealed class AmiiboRewardsDbContext(DbContextOptions<AmiiboRewardsDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>(); public DbSet<AppSetting> AppSettings => Set<AppSetting>(); public DbSet<Amiibo> Amiibo => Set<Amiibo>(); public DbSet<AmiiboCatalogEntry> AmiiboCatalog => Set<AmiiboCatalogEntry>(); public DbSet<Reward> Rewards => Set<Reward>(); public DbSet<RewardLocalization> RewardLocalizations => Set<RewardLocalization>(); public DbSet<AmiiboReward> AmiiboRewards => Set<AmiiboReward>(); public DbSet<Asset> Assets => Set<Asset>(); public DbSet<ImportRun> ImportRuns => Set<ImportRun>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Game>(b => { b.HasIndex(x => x.Code).IsUnique(); b.Property(x => x.Code).HasMaxLength(32); });
        model.Entity<AppSetting>(b => { b.HasIndex(x => x.Key).IsUnique(); b.Property(x => x.Key).HasMaxLength(128); });
        model.Entity<Amiibo>(b => { b.HasIndex(x => new { x.GameId, x.InternalTableId }).IsUnique(); b.Property(x => x.MappingStatus).HasConversion<string>().HasMaxLength(24); b.Property(x => x.MappingSource).HasMaxLength(256); b.HasOne(x => x.Game).WithMany(x => x.Amiibo).HasForeignKey(x => x.GameId); });
        model.Entity<AmiiboCatalogEntry>(b => { b.HasIndex(x => x.SourceFile).IsUnique(); b.HasIndex(x => x.NumberingId); b.HasIndex(x => x.CharacterId); b.HasIndex(x => x.CharacterBaseId); b.Property(x => x.SourceFile).HasMaxLength(1024); b.Property(x => x.DisplayName).HasMaxLength(256); b.Property(x => x.CharacterId).HasMaxLength(8); b.Property(x => x.CharacterBaseId).HasMaxLength(5); b.Property(x => x.SourceSha256).HasMaxLength(64); });
        model.Entity<Reward>(b => { b.HasIndex(x => new { x.GameId, x.InternalId }).IsUnique(); b.HasOne(x => x.Game).WithMany(x => x.Rewards).HasForeignKey(x => x.GameId); b.HasOne(x => x.IconAsset).WithMany().HasForeignKey(x => x.IconAssetId).OnDelete(DeleteBehavior.SetNull); });
        model.Entity<RewardLocalization>(b => { b.HasIndex(x => new { x.RewardId, x.Locale }).IsUnique(); b.Property(x => x.Locale).HasMaxLength(8); b.HasOne(x => x.Reward).WithMany(x => x.Localizations).HasForeignKey(x => x.RewardId).OnDelete(DeleteBehavior.Cascade); });
        model.Entity<Asset>(b => b.HasIndex(x => new { x.GameId, x.InternalId, x.AssetType }).IsUnique());
        model.Entity<AmiiboReward>(b => { b.HasIndex(x => new { x.AmiiboId, x.RewardId, x.Pool, x.EntryIndex }).IsUnique(); b.Property(x => x.Probability).HasPrecision(7, 2); b.Property(x => x.RawWeight).HasPrecision(12, 3); b.Property(x => x.InteractionKind).HasConversion<string>().HasMaxLength(24); b.Property(x => x.OutcomeKind).HasConversion<string>().HasMaxLength(24); b.HasOne(x => x.Amiibo).WithMany(x => x.Rewards).HasForeignKey(x => x.AmiiboId); b.HasOne(x => x.Reward).WithMany(x => x.AmiiboRewards).HasForeignKey(x => x.RewardId); });
        model.Entity<ImportRun>(b => { b.HasIndex(x => new { x.GameId, x.Kind, x.SourceSha256 }).IsUnique(); b.HasOne(x => x.Game).WithMany(x => x.ImportRuns).HasForeignKey(x => x.GameId); });
    }
}
