using AmiiboRewards.Domain;
using Microsoft.EntityFrameworkCore;

namespace AmiiboRewards.Infrastructure;

public sealed class GameLibrary(AmiiboRewardsDbContext db)
{
    private static readonly SemaphoreSlim ScanLock = new(1, 1);

    public async Task<IReadOnlyList<GameLibraryEntry>> ListAsync(string assetsRoot, CancellationToken ct)
    {
        var games = await db.Games.AsNoTracking().OrderBy(g => g.Name).Select(g => new
        {
            g.Code, g.Name, g.RomFsPath, g.LastDetectedAt,
            RewardCount = g.Rewards.Count,
            Locales = g.Rewards.SelectMany(r => r.Localizations).Select(l => l.Locale).Distinct().OrderBy(l => l).ToList()
        }).ToListAsync(ct);
        return games.Select(g =>
        {
            var definition = GameDefinitions.Find(g.Code);
            var present = g.RomFsPath is not null && Directory.Exists(g.RomFsPath);
            var cover = Path.Combine(assetsRoot, g.Code.ToLowerInvariant(), "cover.jpg");
            return new GameLibraryEntry(g.Code, g.Name, definition?.ShortName ?? g.Name, definition?.Theme ?? "neutral",
                definition?.CanImport ?? false, present, g.RomFsPath, g.LastDetectedAt, g.RewardCount, g.Locales,
                File.Exists(cover) ? $"/api/assets/{g.Code.ToLowerInvariant()}/cover" : null,
                g.RewardCount > 0 ? "ready" : definition?.CanImport == true ? "awaiting_import" : "importer_pending");
        }).ToList();
    }

    public async Task<IReadOnlyList<string>> ScanAsync(string root, string assetsRoot, CancellationToken ct)
    {
        await ScanLock.WaitAsync(ct);
        try
        {
            var scan = GameDefinitions.Scan(root);
            var warnings = scan.Warnings.ToList();
            foreach (var group in scan.Detected.GroupBy(item => item.Game.Code))
            {
                var existing = await db.Games.SingleOrDefaultAsync(g => g.Code == group.Key, ct);
                // Keep the previously selected dump when multiple copies are present.
                var detected = group.FirstOrDefault(d => d.RomFsPath == existing?.RomFsPath) ?? group.First();
                if (group.Count() > 1) warnings.Add($"Hay varias copias de {detected.Game.ShortName}; se utilizará {detected.RomFsPath}.");
                if (existing is null) { existing = new Game { Code = detected.Game.Code, Name = detected.Game.Name }; db.Games.Add(existing); }
                existing.RomFsPath = detected.RomFsPath;
                existing.LastDetectedAt = DateTimeOffset.UtcNow;
                try { await GameArtwork.SaveAsync(detected, assetsRoot, ct); }
                catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.ComponentModel.Win32Exception || (ex is OperationCanceledException && !ct.IsCancellationRequested))
                { warnings.Add($"No se pudo guardar la imagen de {detected.Game.ShortName}; verificá los recursos de la RomFS y la instalación de zstd."); }
            }
            await db.SaveChangesAsync(ct);
            return warnings;
        }
        finally { ScanLock.Release(); }
    }
}

public sealed record GameLibraryEntry(string Code, string Name, string ShortName, string Theme, bool CanImport,
    bool DumpAvailable, string? RomFsPath, DateTimeOffset? LastDetectedAt, int RewardCount,
    IReadOnlyList<string> Locales, string? CoverUrl, string Status);
