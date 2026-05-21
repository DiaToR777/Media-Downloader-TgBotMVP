using MediaDownloaderTgBotMVP;
using MediaDownloaderTgBotMVP.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

services.AddSingleton<DownloadWorker>(provider =>
{
    var token = Environment.GetEnvironmentVariable("BOT_TOKEN")!;
    var bot = new Telegram.Bot.TelegramBotClient(token);
    var tempFolder = Path.Combine(AppContext.BaseDirectory, "downloads");
    return new DownloadWorker(bot, tempFolder);
});

var token = Environment.GetEnvironmentVariable("BOT_TOKEN")
    ?? config["BOT_TOKEN"]
    ?? throw new Exception("BOT_TOKEN missing");

services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(token));

services.AddSingleton<DownloadWorker>();
services.AddSingleton<TelegramService>();

var provider = services.BuildServiceProvider();

using (var scope = provider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

var telegramService = provider.GetRequiredService<TelegramService>();
await telegramService.Start();

await Task.Delay(-1);
