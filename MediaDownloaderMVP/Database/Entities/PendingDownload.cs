using MediaDownloaderTgBotMVP.Database.Enums;

namespace MediaDownloaderTgBotMVP.Database.Entities
{
    public class PendingDownload
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public long ChatId { get; set; }
        public int MessageId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? VideoId { get; set; }
        public Platform Platform { get; set; }
        public string? Title { get; set; }
        public long? FilesizeBytes { get; set; }
        public FileType? ChosenFormat { get; set; }
        public MediaQuality? ChosenQuality { get; set; }
        public PendingDownloadStatus Status { get; set; } = PendingDownloadStatus.Analyzing;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);

        public User User { get; set; } = null!;
    }
}
