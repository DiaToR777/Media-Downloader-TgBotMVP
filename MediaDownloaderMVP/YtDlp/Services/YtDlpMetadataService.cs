using MediaDownloaderTgBotMVP.YtDlp.Builders;
using MediaDownloaderTgBotMVP.YtDlp.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MediaDownloaderTgBotMVP.YtDlp.Services
{
    public class YtDlpMetadataService
    {   
        public async Task<VideoMetadata?> GetMetadataAsync(string url, CancellationToken ct)
        {
            var psi = new YtDlpPsiBuilder()
                            .WithDumpJson()
                            .WithUrl(url)
                            .Build();

            using var process = new Process { StartInfo = psi };

            try
            {
                process.Start();

                string jsonOutputTask = await process.StandardOutput.ReadToEndAsync(ct);
                string errorOutputTask = await process.StandardError.ReadToEndAsync(ct);

                await process.WaitForExitAsync(ct);

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[Metadata] Помилка yt-dlp: {errorOutputTask}");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(jsonOutputTask))
                {
                    Console.WriteLine("[Metadata] yt-dlp повернув порожній рядок.");
                    return null;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var metadata = JsonSerializer.Deserialize<VideoMetadata>(jsonOutputTask, options);

                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Metadata] Виключення при отриманні інфи: {ex.Message}");
                return null;
            }
        }

    }
}
