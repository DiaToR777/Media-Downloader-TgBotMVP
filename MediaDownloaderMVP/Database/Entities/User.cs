
namespace MediaDownloaderTgBotMVP.Database.Entities
{
    public class User
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string SubscriptionTier { get; set; } = "free";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

        public ICollection<DownloadHistory> DownloadHistories { get; set; } = new List<DownloadHistory>();
    }
}
