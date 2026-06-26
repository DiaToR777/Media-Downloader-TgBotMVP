using MediaDownloaderTgBotMVP.Database.Enums;
using MediaDownloaderTgBotMVP.Database.Repositories;
using MediaDownloaderTgBotMVP.Helpers;
using MediaDownloaderTgBotMVP.YtDlp.Builders;
using MediaDownloaderTgBotMVP.YtDlp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Threading.Channels;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediaDownloaderTgBotMVP;

public class DownloadWorker : BackgroundService
{
    private readonly Channel<DownloadTask> _queue;
    private readonly ITelegramBotClient _bot;
    private readonly string _tempFolder;
    private readonly int _maxConcurrentDownloads = 3;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly YtDlpMetadataService _metadataService;

    public DownloadWorker(ITelegramBotClient bot, string tempFolder, IServiceScopeFactory scopeFactory, YtDlpMetadataService metadataService)
    {
        _bot = bot;
        _tempFolder = tempFolder;
        _scopeFactory = scopeFactory;
        _metadataService = metadataService;

        if (!Directory.Exists(_tempFolder)) Directory.CreateDirectory(_tempFolder);

        _queue = Channel.CreateBounded<DownloadTask>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ChannelWriter<DownloadTask> Writer => _queue.Writer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverPendingTasksAsync(stoppingToken);

        var workers = new Task[_maxConcurrentDownloads];
        for (int i = 0; i < _maxConcurrentDownloads; i++)
        {
            workers[i] = ProcessQueueAsync(i + 1, stoppingToken);
        }
        Console.WriteLine($"✓ [Worker] Запущено потоків: {_maxConcurrentDownloads}");
        await Task.WhenAll(workers);
    }

    private async Task ProcessQueueAsync(int workerId, CancellationToken ct)
    {
        await foreach (var task in _queue.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                if (task.ChosenFormat != null)
                {
                    await HandleDownloadPhaseAsync(workerId, task, scope, ct);
                }
                else
                {
                    await HandleAnalyzePhaseAsync(workerId, task, scope, ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker {workerId}] ❌ Критическая ошибка: {ex.Message}");
            }
        }
    }

    private async Task HandleAnalyzePhaseAsync(int workerId, DownloadTask task, IServiceScope scope, CancellationToken ct)
    {
        var cacheRepo = scope.ServiceProvider.GetRequiredService<CachedMediaRepository>();
        var pendingRepo = scope.ServiceProvider.GetRequiredService<PendingDownloadRepository>();

        Console.WriteLine($"[Worker {workerId}] 🔍 Анализ: {task.Url}");

        string currentUrl = task.Url;
        var parsedUrl = PlatformDetector.Parse(currentUrl);

        if (parsedUrl.Platform == Platform.Unknown)
        {
            await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "❌ Платформа не підтримується.", cancellationToken: ct);
            return;
        }

        if (parsedUrl.IsShortUrl)
        {
            currentUrl = await ResolveRedirectHelper.ResolveRedirectAsync(currentUrl, ct);
            parsedUrl = PlatformDetector.Parse(currentUrl);
        }

        var metadata = await _metadataService.GetMetadataAsync(currentUrl, ct);
        if (metadata == null)
        {
            await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "❌ Не вдалося отримати інфо про відео.", cancellationToken: ct);
            return;
        }

        if (metadata.GetFilesize() > 50 * 1024 * 1024)
        {
            await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "❌ Відео занадто велике (>50 MB).", cancellationToken: ct);
            return;
        }

        var pending = await pendingRepo.GetAsync(task.PendingId, ct);
        if (pending == null) return;

        pending.Title = metadata.Title;
        pending.VideoId = metadata.Id;
        pending.Platform = parsedUrl.Platform;
        pending.FilesizeBytes = metadata.GetFilesize();
        pending.Status = PendingDownloadStatus.AwaitingChoice;

        await pendingRepo.UpdateAsync(ct);

        var keyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
        {
            new[]
            {
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🎬 Відео", $"dl:video:{pending.Id}"),
                Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🎵 Аудіо", $"dl:audio:{pending.Id}")
            }
        });

        await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId,
            $"📹 *{metadata.Title}*\n\nОберіть формат:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task HandleDownloadPhaseAsync(int workerId, DownloadTask task, IServiceScope scope, CancellationToken ct)
    {
        var cacheRepo = scope.ServiceProvider.GetRequiredService<CachedMediaRepository>();
        var historyRepo = scope.ServiceProvider.GetRequiredService<DownloadHistoryRepository>();
        var pendingRepo = scope.ServiceProvider.GetRequiredService<PendingDownloadRepository>();

        Console.WriteLine($"[Worker {workerId}] 🚀 Запуск скачивания для PendingId: {task.PendingId}");

        var pending = await pendingRepo.GetAsync(task.PendingId, ct);
        if (pending == null) return;

        var history = await historyRepo.CreateAsync(pending.UserId, pending.Url, ct);

        var cached = await cacheRepo.GetByVideoIdAsync(pending.Platform, pending.VideoId!, task.ChosenFormat!.Value, MediaQuality.Standard, ct);
        if (cached != null)
        {
            Console.WriteLine($"[Worker {workerId}] Знайдено в кеші!");
            if (task.ChosenFormat == FileType.Audio)
                await _bot.SendAudio(task.ChatId, cached.FileId, cancellationToken: ct);
            else
                await _bot.SendVideo(task.ChatId, cached.FileId, cancellationToken: ct);

            await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Done, ct, cached.Id);
            await _bot.DeleteMessage(task.ChatId, task.ProgressMessage.MessageId, cancellationToken: ct);

            await pendingRepo.DeleteAsync(pending.Id, ct);
            return;
        }

        await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Pending, ct);

        string taskFolder = Path.Combine(_tempFolder, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(taskFolder);

        try
        {
            var format = task.ChosenFormat == FileType.Audio ? "mp3" : "mp4";
            var psi = new YtDlpPsiBuilder()
                .WithUrl(pending.Url)
                .WithFormat(format)
                .WithOutputPath(taskFolder)
                .Build();

            using var process = new Process { StartInfo = psi };
            process.Start();
            await process.WaitForExitAsync(ct);

            var filePath = Directory.GetFiles(taskFolder).FirstOrDefault();
            if (filePath == null) throw new Exception("Файл не знайдено після yt-dlp.");

            await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "📤 Відправляю в Telegram...", cancellationToken: ct);

            Message sentMessage;
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (task.ChosenFormat == FileType.Audio)
                {
                    sentMessage = await _bot.SendAudio(task.ChatId, InputFile.FromStream(stream, Path.GetFileName(filePath)), cancellationToken: ct);
                }
                else
                {
                    sentMessage = await _bot.SendVideo(task.ChatId, InputFile.FromStream(stream, Path.GetFileName(filePath)), cancellationToken: ct);
                }
            }

            var fileId = task.ChosenFormat == FileType.Audio ? sentMessage.Audio!.FileId : sentMessage.Video!.FileId;
            int newCacheId = await cacheRepo.SaveAsync(pending.Url, pending.VideoId!, pending.Platform, fileId, task.ChosenFormat.Value, MediaQuality.Standard, pending.FilesizeBytes ?? 0);

            await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Done, ct, newCacheId);
            await pendingRepo.DeleteAsync(pending.Id, ct);
            await _bot.DeleteMessage(task.ChatId, task.ProgressMessage.MessageId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Worker {workerId}] ❌ Ошибка скачивания: {ex.Message}");
            await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);
            await pendingRepo.DeleteAsync(pending.Id, ct);
            try { await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, $"❌ Помилка: {ex.Message}", cancellationToken: ct); } catch { }
        }
        finally
        {
            if (Directory.Exists(taskFolder)) Directory.Delete(taskFolder, true);
        }
    }

    private async Task RecoverPendingTasksAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var pendingRepo = scope.ServiceProvider.GetRequiredService<PendingDownloadRepository>();

        var pendingTasks = await pendingRepo.GetStuckAsync(ct);

        if (pendingTasks.Any())
        {
            Console.WriteLine($"[Recover] 🔄 Знайдено {pendingTasks.Count()} завислих завдань. Відновлюю в чергу...");

            foreach (var task in pendingTasks)
            {
                var messageInfo = new MessageInfo(task.MessageId, task.ChatId);

                await _queue.Writer.WriteAsync(new DownloadTask(
                    DbUserId: task.UserId,
                    ChatId: task.ChatId,
                    Url: task.Url,
                    ProgressMessage: messageInfo,
                    PendingId: task.Id,
                    ChosenFormat: task.ChosenFormat
                ), ct);
            }

            Console.WriteLine($"[Recover] ✓ Усі завдання успішно повернуто в чергу.");
        }
    }

}