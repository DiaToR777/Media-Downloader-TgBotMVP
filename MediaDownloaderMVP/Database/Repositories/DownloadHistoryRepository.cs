using MediaDownloaderTgBotMVP.Database.Entities;
using MediaDownloaderTgBotMVP.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediaDownloaderTgBotMVP.Database.Repositories
{
    public class DownloadHistoryRepository
    {
        private readonly AppDbContext _db;

        public DownloadHistoryRepository(AppDbContext db)
        {
            _db = db;
        }
          
        public async Task<DownloadHistory?> CreateAsync(int userId, string sourceUrl, CancellationToken ct)
        {
            var history = new DownloadHistory
            {
                UserId = userId,
                SourceUrl = sourceUrl,
                Status = DownloadStatus.Pending
            };

            _db.DownloadHistories.Add(history);
            await _db.SaveChangesAsync(ct);
            return history;
        }

        public async Task UpdateStatusAsync(int id, DownloadStatus status, CancellationToken ct, int? cachedMediaId = null)
        {
            var history = await _db.DownloadHistories
                .FirstOrDefaultAsync(h => h.Id == id, ct);

            if (history == null) return;

            history.Status = status;

            if (cachedMediaId != null)
                history.CachedMediaId = cachedMediaId;

            await _db.SaveChangesAsync(ct);
        }

    }
}
