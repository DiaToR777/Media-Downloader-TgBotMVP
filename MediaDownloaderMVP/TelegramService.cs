using MediaDownloaderTgBotMVP.Database.Enums;
using MediaDownloaderTgBotMVP.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MediaDownloaderTgBotMVP
{
    public class TelegramService
    {
        private readonly ITelegramBotClient _bot;
        private readonly long _adminId;

        private readonly DownloadWorker _downloadWorker;
        private readonly IServiceScopeFactory _scopeFactory;
        public TelegramService(DownloadWorker downloadWorker, ITelegramBotClient bot, IServiceScopeFactory scopeFactory)
        {
            _bot = bot;

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

            _bot.StartReceiving(
                HandleUpdate,
                HandleError,
                new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
                },
                cancellationToken: cts.Token
            );

            Console.WriteLine("Бот слухає повідомлення...");
        }

        private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.CallbackQuery is { } callbackQuery)
            {
                await HandleCallbackQuery(callbackQuery, ct);
                return;
            }

            if (update.Message?.Text is not { } text || update.Message.From is not { } tgUser) return;

            var tgUserId = tgUser.Id;

            var chatId = update.Message.Chat.Id;

            var username = tgUser.Username;
            var firstName = tgUser.FirstName;

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {update.Message.Chat.FirstName ?? "User"}, userId {{ {update.Message.Chat.Id} }} username {{ {update.Message.Chat.Username ?? "null"} }} : {text}");

            using var scope = _scopeFactory.CreateScope();
            var userRepo = scope.ServiceProvider.GetRequiredService<UserRepository>();

            var dbUser = await userRepo.GetOrCreateAsync(tgUserId, username, firstName);

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
                await StartDownloading(dbUser.Id, chatId, text, ct);
            }
        }

        private async Task StartDownloading(int userId, long chatId, string url, CancellationToken ct)
        {
            Message progressMessage = await _bot.SendMessage(chatId, "⏳ Додано в чергу завантаження...", cancellationToken: ct);

            using var scope = _scopeFactory.CreateScope();
            var pendingRepo = scope.ServiceProvider.GetRequiredService<PendingDownloadRepository>();

            var pending = await pendingRepo.CreateAsync(userId, chatId, progressMessage.MessageId, url,  ct);

            var task = new DownloadTask(userId, chatId, url, new MessageInfo( progressMessage.Id, chatId), pending.Id, pending.ChosenFormat);

            if (!_downloadWorker.Writer.TryWrite(task))
            {
                Console.WriteLine("❌ Черга переповнена, спробуйте пізніше.");
                await _bot.EditMessageText(chatId, progressMessage.MessageId, "❌ Черга переповнена, спробуйте пізніше.", cancellationToken: ct);
                await pendingRepo.DeleteAsync(pending.Id, ct);
            }
        }

        private async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
        {
            // data формат: "dl:video:123" или "dl:audio:123"
            var data = callbackQuery.Data;
            if (data == null || !data.StartsWith("dl:")) return;

            var parts = data.Split(':');
            if (parts.Length != 3 || !int.TryParse(parts[2], out int pendingId)) return;

            var formatStr = parts[1]; 

            var chatId = callbackQuery.Message!.Chat.Id;
            int messageId = callbackQuery.Message.MessageId;

            await _bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

            using var scope = _scopeFactory.CreateScope();
            var pendingRepo = scope.ServiceProvider.GetRequiredService<PendingDownloadRepository>();

            var pending = await pendingRepo.GetAsync(pendingId, ct);
            if (pending == null || pending.Status != PendingDownloadStatus.AwaitingChoice)
            {
                await _bot.SendMessage(chatId, "❌ Сесія застаріла, надішліть посилання ще раз.", cancellationToken: ct);
                return;
            }

            pending.ChosenFormat = formatStr == "audio" ? Database.Enums.FileType.Audio : Database.Enums.FileType.Video;
            pending.Status = PendingDownloadStatus.Downloading;
            await pendingRepo.UpdateAsync(ct);

            var progressMessage = await _bot.EditMessageText(chatId, messageId, "⏳ Завантажую медіа, зачекайте...", cancellationToken: ct); 

            var task = new DownloadTask(pending.UserId, chatId, pending.Url, new MessageInfo(messageId, chatId), pending.Id, pending.ChosenFormat);

            if (!_downloadWorker.Writer.TryWrite(task))
                await _bot.EditMessageText(chatId, progressMessage.MessageId, "❌ Черга переповнена.", cancellationToken: ct);
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
