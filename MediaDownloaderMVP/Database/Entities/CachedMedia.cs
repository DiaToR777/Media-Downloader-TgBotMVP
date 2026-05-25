using MediaDownloaderTgBotMVP.Database.Enums;
namespace MediaDownloaderTgBotMVP.Database.Entities
{
    public class CachedMedia
    {
        public int Id { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
        public Platform Platform { get; set; } 
        public string FileId { get; set; } = string.Empty;
        public FileType FileType { get; set; } = FileType.Video;
        public MediaQuality Quality { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DownloadHistory> DownloadHistories { get; set; } = new List<DownloadHistory>();
    }
}
