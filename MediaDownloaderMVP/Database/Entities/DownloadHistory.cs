
namespace MediaDownloaderTgBotMVP.Database.Entities
{
    public class DownloadHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? CachedMediaId { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public CachedMedia? CachedMedia { get; set; }
    }
}
