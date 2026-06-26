using MediaDownloaderTgBotMVP.Database.Entities;
using MediaDownloaderTgBotMVP.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediaDownloaderTgBotMVP.Database.Repositories
{
    public class CachedMediaRepository
    {
        private readonly AppDbContext _db;
        public CachedMediaRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CachedMedia?> FindAsync(string sourceUrl, Enums.FileType fileType, MediaQuality quality)
        {
            return await _db.CachedMedias
                .FirstOrDefaultAsync(c =>
                    c.SourceUrl == sourceUrl &&
                    c.FileType == fileType &&
                    c.Quality == quality);
        }

        public async Task<int> SaveAsync(string url, string videoId, Platform platform, string fileId, FileType format, MediaQuality quality, long fileSizeBytes, CancellationToken ct = default)
        {
            var media = new CachedMedia
            {
                SourceUrl = url,
                VideoId = videoId,
                Platform = platform,
                FileId = fileId,
                FileType = format,
                Quality = quality,
                FileSizeBytes = fileSizeBytes,
                CreatedAt = DateTime.UtcNow
            };

            _db.CachedMedias.Add(media);

            try
            {
                await _db.SaveChangesAsync(ct);
                return media.Id;
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                _db.Entry(media).State = EntityState.Detached;
                var existing = await GetByVideoIdAsync(platform, videoId, format, quality, ct);
                return existing?.Id ?? throw ex;
            }
        }
        public async Task<CachedMedia?> GetByVideoIdAsync(Platform platform, string videoId, FileType fileType, MediaQuality quality, CancellationToken ct)
        {
            return await _db.CachedMedias.FirstOrDefaultAsync(c =>
                c.Platform == platform &&
                c.VideoId == videoId &&
                c.FileType == fileType &&
                c.Quality == quality, ct);
        }
    }
}   
