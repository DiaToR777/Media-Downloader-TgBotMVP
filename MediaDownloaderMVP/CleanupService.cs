using MediaDownloaderTgBotMVP.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MediaDownloaderTgBotMVP
{
    public class CleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public CleanupService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct);

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<PendingDownloadRepository>();

                int deleted = await repo.DeleteExpiredAsync(ct);
                if (deleted > 0)
                    Console.WriteLine($"[Cleanup] Видалено {deleted} прострочених записів");
            }
        }

    }
}
