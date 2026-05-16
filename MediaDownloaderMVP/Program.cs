using MediaDownloaderTgBotMVP;

TelegramService telegramService = new();

await telegramService.Start();

await Task.Delay(-1);