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

        if (!Directory.Exists(_tempFolder))
            Directory.CreateDirectory(_tempFolder);

        _queue = Channel.CreateBounded<DownloadTask>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = new Task[_maxConcurrentDownloads];

        for (int i = 0; i < _maxConcurrentDownloads; i++)
        {
            int workerId = i + 1;
            workers[i] = ProcessQueueAsync(workerId, stoppingToken);
        }

        Console.WriteLine($"✓ [BackgroundService] Запущено пул воркерів ({_maxConcurrentDownloads} паралельних потоків)");

        return Task.WhenAll(workers);
    }

    public ChannelWriter<DownloadTask> Writer => _queue.Writer;


    private async Task ProcessQueueAsync(int workerId, CancellationToken ct)
    {
        await foreach (var task in _queue.Reader.ReadAllAsync(ct))
        {
            Console.WriteLine($"[Worker {workerId}] Взяв у роботу: {task.Url} для ChatId: {task.ChatId}");

            using var scope = _scopeFactory.CreateScope();
            var cacheRepo = scope.ServiceProvider.GetRequiredService<CachedMediaRepository>();
            var historyRepo = scope.ServiceProvider.GetRequiredService<DownloadHistoryRepository>();

            var history = await historyRepo.CreateAsync(task.DbUserId, task.Url, ct);

            string currentUrl = task.Url;
            var parsedUrl = PlatformDetector.Parse(task.Url);

            if (parsedUrl.Platform == Platform.Unknown)
            {
                Console.WriteLine($"[Worker {workerId}] ⚠️ Невідома платформа для {task.Url}");
                await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);
                try
                {
                    await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "❌ Ця платформа не підтримується.", cancellationToken: ct);
                }
                catch { }
                continue;
            }

            if (parsedUrl.IsShortUrl)
            {
                Console.WriteLine($"[Worker {workerId}] ⚙️ Виявлено коротке посилання {parsedUrl.Platform}. Розгортаємо...");
                currentUrl = await ResolveRedirectHelper.ResolveRedirectAsync(currentUrl, ct);

                // Парсим длинную ссылку заново, чтобы извлечь реальный ID видео
                parsedUrl = PlatformDetector.Parse(currentUrl);
            }

            string? finalVideoId = parsedUrl.VideoId;

            if (!string.IsNullOrEmpty(finalVideoId))
            {
                var cached = await cacheRepo.GetByVideoIdAsync(parsedUrl.Platform, finalVideoId, FileType.Video, MediaQuality.Standard, ct);
                if (cached != null)
                {
                    Console.WriteLine($"[Worker {workerId}] 🚀 Швидкий кэш знайдено! Відправляємо file_id миттєво.");
                    await SendCachedVideoAsync(task, cached.FileId, history.Id, cached.Id, historyRepo, ct);
                    continue;
                }
            }

            await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Pending, ct);

            await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, 
                "🔍 Аналізую посилання...", cancellationToken: ct);

            var metadata = await _metadataService.GetMetadataAsync(currentUrl, ct);
            if (metadata == null)
            {
                Console.WriteLine($"[Worker {workerId}] ⚠️ Не вдалося отримати метадані для {task.Url}");

                await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);
                try
                {
                    await _bot.EditMessageText(
                        task.ChatId,
                        task.ProgressMessage.MessageId,
                        "❌ Не вдалося отримати інформацію про відео. " +
                        "Можливо, посилання бите або сервіс тимчасово недоступний.",
                        cancellationToken: ct
                    );
                }
                catch { }
                continue;
            }

            string strictVideoId = metadata.Id;

            if (strictVideoId != finalVideoId)
            {
                var cached = await cacheRepo.GetByVideoIdAsync(parsedUrl.Platform, strictVideoId, FileType.Video, MediaQuality.Standard, ct);
                if (cached != null)
                {
                    Console.WriteLine($"[Worker {workerId}] Строгий кэш знайдено після аналізу метаданих!");
                    await SendCachedVideoAsync(task, cached.FileId, history.Id, cached.Id, historyRepo, ct);
                    continue;
                }
            }

            long fileSizeInBytes = metadata.GetFilesize();
            if (fileSizeInBytes > 50 * 1024 * 1024)
            {
                double fileSizeMb = Math.Round((double)fileSizeInBytes / 1024 / 1024, 2);

                Console.WriteLine($"[Worker {workerId}] ⚠️ Відео занадто велике ({fileSizeMb} MB). {task.Url}");

                //TODO Compression 
                await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);

                try
                {
                    await _bot.EditMessageText(
                        task.ChatId,
                        task.ProgressMessage.MessageId,
                        $"❌ Відео занадто велике ({fileSizeMb} MB). " +
                        $"Максимальний розмір — 50 MB.",
                        cancellationToken: ct
                    );
                }
                catch { }

                continue;
            }

            await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, $"⏳ Завантажую відео: {metadata.Title}...", cancellationToken: ct);

            string taskFolder = Path.Combine(_tempFolder, Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(taskFolder);

                using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                var psi = new YtDlpPsiBuilder()
                    .WithUrl(currentUrl)
                    .WithFormat("mp4")
                    .WithOutputPath(taskFolder)
                    .Build(); // TODO: format...

                using (var process = new Process { StartInfo = psi })
                {
                    downloadCts.CancelAfter(TimeSpan.FromMinutes(3));
                    process.Start();

                    using var registration = downloadCts.Token.Register(() =>
                    {
                        try { process.Kill(entireProcessTree: true); } catch { }
                    });

                    string stderr = await process.StandardError.ReadToEndAsync(downloadCts.Token);
                    await process.WaitForExitAsync(downloadCts.Token);

                    Console.WriteLine($"[Worker {workerId}] yt-dlp stderr: {stderr}");

                    if (process.ExitCode != 0)
                        throw new Exception($"yt-dlp помилка: {stderr}");
                }

                var filePath = Directory.GetFiles(taskFolder).FirstOrDefault();
                if (filePath == null)
                {
                    await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);

                    try
                    {
                        await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "❌ Не вдалося знайти завантажений файл.", cancellationToken: ct);
                    }
                    catch { }

                    Console.WriteLine($"[Worker {workerId}] Файл не знайдено.");
                    continue;
                }

                long fileSize = new FileInfo(filePath).Length;
                if (fileSize > 50 * 1024 * 1024)
                {
                    double fileSizeMb = Math.Round((double)fileSize / 1024 / 1024, 2);

                    await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);

                    Console.WriteLine($"[Worker {workerId}] Скачаний файл виявився занадто великим ({fileSizeMb}MB).");

                    try
                    {
                        await _bot.EditMessageText(
                            task.ChatId,
                            task.ProgressMessage.MessageId, $"❌ Відео занадто велике ({fileSizeMb} MB). " +
                            $"Максимальний розмір — 50 MB.", cancellationToken: ct);

                    }
                    catch { }

                    continue;
                }
                await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "📤 Надсилаю відео...", cancellationToken: ct);

                Message sentMessage;

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                {
                    sentMessage = await _bot.SendVideo(
                        chatId: task.ChatId,
                        video: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        cancellationToken: ct
                    );
                }

                if (sentMessage.Video?.FileId != null)
                {
                    int newCacheId = await cacheRepo.SaveAsync(
                                            sourceUrl: currentUrl,
                                            videoId: strictVideoId,
                                            platform: parsedUrl.Platform,
                                            fileId: sentMessage.Video.FileId,
                                            fileType: FileType.Video,
                                            quality: MediaQuality.Standard,
                                            fileSizeBytes: fileSize
                                        );

                    await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Done, ct, newCacheId);

                    Console.WriteLine($"[Worker {workerId}] Збережено в кэш: {sentMessage.Video.FileId}");
                }
                else
                {
                    await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Done, ct);
                }

                try { await _bot.DeleteMessage(task.ChatId, task.ProgressMessage.MessageId, cancellationToken: ct); } catch { }
            }
            catch (OperationCanceledException)
            {
                await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);
                try { await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "⏱ Час очікування вийшов.", cancellationToken: ct); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker {workerId}] ❌ {ex.Message}");

                await historyRepo.UpdateStatusAsync(history.Id, DownloadStatus.Failed, ct);
                try
                {
                    await _bot.EditMessageText(task.ChatId,
                        task.ProgressMessage.MessageId,
                        $"❌ Помилка: {ex.Message}",
                        cancellationToken: ct);
                }
                catch { }
            }
            finally
            {
                if (Directory.Exists(taskFolder))
                    try { Directory.Delete(taskFolder, recursive: true); } catch { }
            }
        }
    }

    private async Task SendCachedVideoAsync(DownloadTask task, string fileId, int historyId, int cacheId, DownloadHistoryRepository historyRepo, CancellationToken ct)
    {
        try
        {
            await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, 
                "📤 Надсилаю відео...", cancellationToken: ct);
        }
        catch { }

        await _bot.SendVideo(task.ChatId, fileId, cancellationToken: ct);

        try
        {
            await _bot.DeleteMessage(task.ChatId, 
                task.ProgressMessage.MessageId, cancellationToken: ct);
        }
        catch { }

        await historyRepo.UpdateStatusAsync(historyId, DownloadStatus.Done, ct, cacheId);
    }
}
