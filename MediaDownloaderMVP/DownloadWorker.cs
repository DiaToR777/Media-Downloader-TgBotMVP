using MediaDownloaderTgBotMVP.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Threading.Channels;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MediaDownloaderTgBotMVP;

public class DownloadWorker
{
    private readonly Channel<DownloadTask> _queue;
    private readonly ITelegramBotClient _bot;
    private readonly string _tempFolder;
    private readonly int _maxConcurrentDownloads = 3;
    private readonly IServiceScopeFactory _scopeFactory;

    public DownloadWorker(ITelegramBotClient bot, string tempFolder, IServiceScopeFactory scopeFactory)
    {
        _bot = bot;
        _tempFolder = tempFolder;
        _scopeFactory = scopeFactory;

        if (!Directory.Exists(_tempFolder))
            Directory.CreateDirectory(_tempFolder);

        _queue = Channel.CreateBounded<DownloadTask>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ChannelWriter<DownloadTask> Writer => _queue.Writer;

    public void Start(CancellationToken ct)
    {
        for (int i = 0; i < _maxConcurrentDownloads; i++)
        {
            int workerId = i + 1;
            Task.Run(() => ProcessQueueAsync(workerId, ct), ct);
        }
        Console.WriteLine($"✓ Запущено пул воркерів ({_maxConcurrentDownloads} паралельних потоків)");
    }

    private async Task ProcessQueueAsync(int workerId, CancellationToken ct)
    {
        await foreach (var task in _queue.Reader.ReadAllAsync(ct))
        {
            Console.WriteLine($"[Worker {workerId}] Взяв у роботу: {task.Url} для ChatId: {task.ChatId}");

            using var scope = _scopeFactory.CreateScope();
            var cacheRepo = scope.ServiceProvider.GetRequiredService<CachedMediaRepository>();

            var cached = await cacheRepo.FindAsync(task.Url, "video", "720p");
            if (cached != null)
            {
                Console.WriteLine($"[Worker {workerId}] Кэш знайдено! Відправляємо file_id");
                await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "📤 Надсилаю відео...", cancellationToken: ct);
                await _bot.SendVideo(task.ChatId, cached.FileId, cancellationToken: ct);
                await _bot.DeleteMessage(task.ChatId, task.ProgressMessage.MessageId, cancellationToken: ct);
                continue;
            }

            string taskFolder = Path.Combine(_tempFolder, Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(taskFolder);

                using var downloadCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                downloadCts.CancelAfter(TimeSpan.FromMinutes(3));

                var psi = new YtDlpPsiBuilder()
                    .WithUrl(task.Url)
                    .WithFormat("mp4")
                    .WithOutputPath(taskFolder)
                    .Build();

                using (var process = new Process { StartInfo = psi })
                {
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
                    throw new FileNotFoundException("Файл не знайдено.");

                var fileSize = new FileInfo(filePath).Length;
                if (fileSize > 50 * 1024 * 1024)
                    throw new Exception($"Відео занадто велике ({fileSize / 1024 / 1024}MB). Максимум 50MB.");

                await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "📤 Надсилаю відео...", cancellationToken: ct);

                using (var stream = File.OpenRead(filePath))
                {
                    var sentMessage = await _bot.SendVideo(
                        chatId: task.ChatId,
                        video: InputFile.FromStream(stream, Path.GetFileName(filePath)),
                        cancellationToken: ct
                    );

                    if (sentMessage.Video?.FileId != null)
                    {
                        await cacheRepo.SaveAsync(
                            sourceUrl: task.Url,
                            platform: task.Platform,
                            fileId: sentMessage.Video.FileId,
                            fileType: "video", //TODO Filetype
                            quality: "720p", //TODO Quality
                            fileSizeBytes: fileSize
                            //TODO videoId
                        );
                        Console.WriteLine($"[Worker {workerId}] Збережено в кэш: {sentMessage.Video.FileId}");

                    }
                }

                await _bot.DeleteMessage(task.ChatId, task.ProgressMessage.MessageId, cancellationToken: ct);
            }
            catch (OperationCanceledException)
            {
                try { await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, "⏱ Час очікування вийшов.", cancellationToken: ct); } catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Worker {workerId}] ❌ {ex.Message}");
                try { await _bot.EditMessageText(task.ChatId, task.ProgressMessage.MessageId, $"❌ Помилка: {ex.Message}", cancellationToken: ct); } catch { }
            }
            finally
            {
                if (Directory.Exists(taskFolder))
                    try { Directory.Delete(taskFolder, recursive: true); } catch { }
            }
        }
    }
}
