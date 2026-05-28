using MediaDownloaderTgBotMVP;
using MediaDownloaderTgBotMVP.Database;
using MediaDownloaderTgBotMVP.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? throw new Exception("CONNECTION_STRING missing");

        var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
            ?? throw new Exception("BOT_TOKEN missing");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

        services.AddScoped<UserRepository>();
        services.AddScoped<CachedMediaRepository>();
        services.AddScoped<DownloadHistoryRepository>();

        services.AddSingleton<DownloadWorker>(provider =>
        {
            var bot = provider.GetRequiredService<ITelegramBotClient>();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var tempFolder = Path.Combine(AppContext.BaseDirectory, "downloads");
            return new DownloadWorker(bot, tempFolder, scopeFactory);
        });

        services.AddHostedService(provider => provider.GetRequiredService<DownloadWorker>());

        services.AddSingleton<TelegramService>();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

var telegramService = host.Services.GetRequiredService<TelegramService>();
await telegramService.Start();

await host.RunAsync();