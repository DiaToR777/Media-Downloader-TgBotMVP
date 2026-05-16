using System.Diagnostics;
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

        private string _taskFolder;

        private readonly string _tempFolder;
        public TelegramService()
        {
            var token = Environment.GetEnvironmentVariable("BOT_TOKEN");
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException("BOT_TOKEN missing");
            _bot = new TelegramBotClient(token);

            var adminIdStr = Environment.GetEnvironmentVariable("ADMIN_TG_ID");
            if (string.IsNullOrEmpty(adminIdStr))
                throw new ArgumentNullException("ADMIN_TG_ID missing");
            _adminId = long.Parse(adminIdStr);

            _tempFolder = Path.Combine(AppContext.BaseDirectory, "downloads");
            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            }
        }
        public async Task Start()
        {
            var me = await _bot.GetMe();
            Console.WriteLine($"✓ Бот @{me.Username} запущено");

            var cts = new CancellationTokenSource();

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

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {update.Message.Chat.FirstName ?? "User"}, userId {{ {update.Message.Chat.Id} }} username {{ {update.Message.Chat.Username ?? "null"} }} : {text}");

            if (text == "/start")
            {
                await bot.SendMessage(chatId, "Привіт! Надішли мені посилання на TikTok, і я завантажу відео.", cancellationToken: ct);
            }
            else if (text == "/help")
            {
                await bot.SendMessage(chatId, "Вас вітає MediaDownloader!\n" +
                    "Просто відправ посилання на відео з TikTok, і я завантажу його для вас.", cancellationToken: ct);
            }
            else if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
            {
                await StartDownloading(chatId, text, ct);
            }
        }


        private async Task StartDownloading(long chatId, string url, CancellationToken ct)
        {
            Message progressMessage = await _bot.SendMessage(chatId, "⏳ Завантажую відео з TikTok...", cancellationToken: ct);

            try
            {
                _taskFolder = Path.Combine(_tempFolder, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_taskFolder);

                using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                downloadCts.CancelAfter(TimeSpan.FromMinutes(3));
                
                var psi = new YtDlpPsiBuilder()
                    .WithUrl(url)
                    .WithFormat("mp4")
                    .WithOutputPath(_taskFolder)
                    .Build();

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();

                    downloadCts.Token.Register(() =>
                    {
                        try { process.Kill(); } catch { }
                    });

                    string stderr = await process.StandardError.ReadToEndAsync(downloadCts.Token);
                    await process.WaitForExitAsync(downloadCts.Token);

                    Console.WriteLine($"yt-dlp stderr: {stderr}");

                    if (process.ExitCode != 0)
                        throw new Exception($"yt-dlp помилка: {stderr}");
                }

                var filePath = Directory.GetFiles(_taskFolder).FirstOrDefault();
                Console.WriteLine($"Знайдено файл: {filePath ?? "null"}");

                if (filePath == null)
                    throw new FileNotFoundException("Файл не був знайдений після завантаження.");

                var fileSize = new FileInfo(filePath).Length;
                if (fileSize > 50 * 1024 * 1024)
                    throw new Exception($"Відео занадто велике ({fileSize / 1024 / 1024}MB). Максимум 50MB.");

                await _bot.EditMessageText(chatId, progressMessage.MessageId, "📤 Надсилаю відео...", cancellationToken: ct);

                using (var stream = File.OpenRead(filePath))
                {
                    await _bot.SendVideo(
                        chatId: chatId,
                        video: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        cancellationToken: ct
                    );
                }

                await _bot.DeleteMessage(chatId, progressMessage.MessageId, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                await _bot.EditMessageText(chatId, progressMessage.MessageId, "⏱ Час очікування вийшов. Спробуйте ще раз.", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{chatId}] ❌ Критична помилка: {ex.Message}");

                await _bot.SendMessage(chatId, "❌ Не вдалося завантажити відео. Спробуйте пізніше.", cancellationToken: ct);

                string rawStack = ex.ToString();
                string safeStack = rawStack.Length > 2000 ? rawStack[..2000] + "\n... [обрізано]" : rawStack;

                await _bot.SendMessage(
                    chatId: _adminId,
                    text: $"🚨 **КРИТИЧНИЙ ЗБІЙ**\nПомилка: `{ex.Message}`\n\nСтек:\n`{safeStack}`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            finally
            {
                if (Directory.Exists(_taskFolder))
                    Directory.Delete(_taskFolder, recursive: true);
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
