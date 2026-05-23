using Microsoft.EntityFrameworkCore;
using MediaDownloaderTgBotMVP.Database.Entities;

namespace MediaDownloaderTgBotMVP.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<CachedMedia> CachedMedias { get; set; }
        public DbSet<DownloadHistory> DownloadHistories { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.TelegramId).IsUnique();
                entity.Property(u => u.SubscriptionTier).HasMaxLength(50);
            });

            modelBuilder.Entity<CachedMedia>(entity =>
            {
                entity.Property(c => c.SourceUrl).HasMaxLength(2000);
                entity.Property(c => c.Quality).HasMaxLength(50);
                entity.Property(c => c.FileType).HasMaxLength(50);

                entity.HasIndex(c => new { c.SourceUrl, c.Quality, c.FileType });
            });

            modelBuilder.Entity<DownloadHistory>(entity =>
            {
                entity.Property(h => h.Status).HasMaxLength(50);
                entity.Property(h => h.SourceUrl).HasMaxLength(2000);

                entity.HasIndex(h => h.UserId);
            });
        }
    }
}