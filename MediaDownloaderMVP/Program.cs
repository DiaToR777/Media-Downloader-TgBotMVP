using MediaDownloaderTgBotMVP;
using MediaDownloaderTgBotMVP.Database;
using MediaDownloaderTgBotMVP.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

var services = new ServiceCollection();

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? throw new Exception("CONNECTION_STRING missing");

var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
    ?? throw new Exception("BOT_TOKEN missing");

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

services.AddSingleton<DownloadWorker>(provider =>
{
    var bot = provider.GetRequiredService<ITelegramBotClient>();
    var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    var tempFolder = Path.Combine(AppContext.BaseDirectory, "downloads");
    return new DownloadWorker(bot, tempFolder, scopeFactory);
});

services.AddSingleton<TelegramService>();

services.AddScoped<UserRepository>();
services.AddScoped<CachedMediaRepository>();
services.AddScoped<DownloadHistoryRepository>();

var provider = services.BuildServiceProvider();

using (var scope = provider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

var telegramService = provider.GetRequiredService<TelegramService>();
await telegramService.Start();
await Task.Delay(-1);