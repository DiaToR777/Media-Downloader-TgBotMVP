using MediaDownloaderTgBotMVP.Database.Entities;
using MediaDownloaderTgBotMVP.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

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

        public async Task<int> SaveAsync(string sourceUrl, string videoId, Platform platform, string fileId, Enums.FileType fileType, MediaQuality quality, long fileSizeBytes)
        {
            var cached = new CachedMedia
            {
                SourceUrl = sourceUrl,
                VideoId = videoId,
                Platform = platform,
                FileId = fileId,
                FileType = fileType,
                Quality = quality,
                FileSizeBytes = fileSizeBytes
            };
            _db.CachedMedias.Add(cached);
            await _db.SaveChangesAsync();

            return cached.Id;
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
