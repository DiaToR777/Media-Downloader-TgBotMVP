using System.Diagnostics;

namespace MediaDownloaderTgBotMVP
{
    public class YtDlpPsiBuilder
    {
        private string _url = string.Empty;
        private string _format = "mp4";

        private string? _outputDirectory = null;

        /// <summary>
        /// Указывает URL для загрузки.
        /// </summary>
        public YtDlpPsiBuilder WithUrl(string url)
        {
            _url = url;
            return this;
        }

        /// <summary>
        /// Устанавливает формат загрузки: "mp3" или "mp4".
        /// </summary>
        public YtDlpPsiBuilder WithFormat(string format)
        {
            _format = format;
            return this;
        }

        public YtDlpPsiBuilder WithOutputPath(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
            return this;
        }

        /// <summary>
        /// Собирает и возвращает ProcessStartInfo.
        /// </summary>
        public ProcessStartInfo Build()
        {
            string externalArgs = string.Empty;
            string formatArgs = _format switch
            {
                "mp3" => "-x --audio-format mp3",
                "mp4" => "-f bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4] --merge-output-format mp4",
                _ => throw new InvalidOperationException("Неизвестный формат: " + _format)
            };

            string outputPath = _outputDirectory != null
                ? $"--output \"{_outputDirectory}/%(id)s.%(ext)s\" "
                : string.Empty;

            string arguments = $"{externalArgs}{formatArgs} {outputPath}\"{_url}\"";

            return new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp",
                Arguments = arguments,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
    } 
}
