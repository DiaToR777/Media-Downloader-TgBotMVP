using MediaDownloaderTgBotMVP.Database.Repositories;
using MediaDownloaderTgBotMVP.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MediaDownloaderTgBotMVP
{
    public class TelegramService
    {
        private TelegramBotClient _bot;
        private readonly long _adminId;

        private readonly DownloadWorker _downloadWorker;
        private readonly IServiceScopeFactory _scopeFactory;
        public TelegramService(DownloadWorker downloadWorker, ITelegramBotClient bot, IServiceScopeFactory scopeFactory)
        {
            _bot = (TelegramBotClient)bot;

            var adminIdStr = Environment.GetEnvironmentVariable("ADMIN_TG_ID");
            if (string.IsNullOrEmpty(adminIdStr))
                throw new ArgumentNullException("ADMIN_TG_ID missing");
            _adminId = long.Parse(adminIdStr);

            _downloadWorker = downloadWorker;
            _scopeFactory = scopeFactory;
        }

        public async Task Start()
        {
            var me = await _bot.GetMe();
            Console.WriteLine($"✓ Бот @{me.Username} запущено");

            var cts = new CancellationTokenSource();

            _downloadWorker.Start(cts.Token);

            _bot.StartReceiving(
                HandleUpdate,
                HandleError,
                new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
                cancellationToken: cts.Token
            );

            Console.WriteLine("Бот слухає повідомлення...");
        }

        private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message?.Text is not { } text) return;

            var chatId = update.Message.Chat.Id;
            var username = update.Message.Chat.Username;
            var firstName = update.Message.Chat.FirstName;

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {update.Message.Chat.FirstName ?? "User"}, userId {{ {update.Message.Chat.Id} }} username {{ {update.Message.Chat.Username ?? "null"} }} : {text}");

            using var scope = _scopeFactory.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<UserRepository>();
            await userRepo.GetOrCreateAsync(chatId, username, firstName);

            if (text == "/start")
            {
                await bot.SendMessage(chatId, "Привіт! Надішли мені посилання, і я завантажу відео.", cancellationToken: ct);
            }
            else if (text == "/help")
            {
                await bot.SendMessage(chatId, "Вас вітає MediaDownloader!\n" +
                    "Просто відправ посилання на відео, і я завантажу його для вас.", cancellationToken: ct);
            }
            else if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
            {
                await StartDownloading(chatId, text, ct);
            }
        }

        private async Task StartDownloading(long chatId, string url, CancellationToken ct)
        {
            Message progressMessage = await _bot.SendMessage(chatId, "⏳ Додано в чергу завантаження...", cancellationToken: ct);

            var mediaPlatform = PlatformDetector.Detect(url);

            var task = new DownloadTask(chatId, url, progressMessage, mediaPlatform);

            if (!_downloadWorker.Writer.TryWrite(task))
            {
                await _bot.EditMessageText(chatId, progressMessage.MessageId, "❌ Черга переповнена, спробуйте пізніше.", cancellationToken: ct);
            }
        }

        public async Task HandleError(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            Console.WriteLine($"❌ Помилка бота: {ex.Message}");

            var logMessage = $"⛔️ ПОМИЛКА ПОЛІНГУ! Бот може бути нестабільним.\n" +
                             $"Помилка: **{ex.Message}**\n" +
                             $"Стек: ```{ex.StackTrace ?? "Стеку немає"}...```";

            await bot.SendMessage(
                chatId: _adminId,
                text: logMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
        }

    }
}
