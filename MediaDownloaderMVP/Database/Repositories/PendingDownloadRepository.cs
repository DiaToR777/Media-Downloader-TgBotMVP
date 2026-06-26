using MediaDownloaderTgBotMVP.Database.Entities;
using MediaDownloaderTgBotMVP.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediaDownloaderTgBotMVP.Database.Repositories
{
    public class PendingDownloadRepository
    {
        private readonly AppDbContext _db;

        public PendingDownloadRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PendingDownload> CreateAsync(int userId, long chatId, int messageId, string url, CancellationToken ct = default)
        {
            var pending = new PendingDownload
            {
                UserId = userId,
                ChatId = chatId,
                MessageId = messageId,
                Url = url
            };
            _db.PendingDownloads.Add(pending);
            await _db.SaveChangesAsync(ct);
            return pending;
        }

        public async Task<PendingDownload?> GetAsync(int id, CancellationToken ct = default)
        {
            return await _db.PendingDownloads.FindAsync([id], ct);
        }

        public async Task UpdateAsync(CancellationToken ct) => await _db.SaveChangesAsync(ct); //only with tracked entities

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            await _db.PendingDownloads
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync(ct);
        }

        public async Task<List<PendingDownload>> GetStuckAsync(CancellationToken ct = default)
        {
            return await _db.PendingDownloads
                .Where(p => p.Status == PendingDownloadStatus.Downloading)
                .ToListAsync(ct);
        }
        public async Task<int> DeleteExpiredAsync(CancellationToken ct = default)
        {
            return await _db.PendingDownloads
                .Where(p => p.ExpiresAt < DateTime.UtcNow
                    || p.Status == PendingDownloadStatus.Done
                    || p.Status == PendingDownloadStatus.Failed)
                .ExecuteDeleteAsync(ct);
        }

    }
}
