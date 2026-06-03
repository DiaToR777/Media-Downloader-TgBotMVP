using System.Diagnostics;

namespace MediaDownloaderTgBotMVP.YtDlp.Builders
{
    public class YtDlpPsiBuilder
    {
        private string _url = string.Empty;
        private string _format = "mp4";
        private bool _dumpJson = false;

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

        public YtDlpPsiBuilder WithDumpJson()
        {
            _dumpJson = true;
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
            string arguments;
            bool redirectOutput;

            if (_dumpJson)
            {
                arguments = $"--dump-json \"{_url}\"";
                redirectOutput = true; 
            }
            else
            {

                string externalArgs = string.Empty;
                string formatArgs = _format switch
                {
                    "mp3" => "-x --audio-format mp3",
                    "mp4" => "-f \"bestvideo[vcodec=h264]+bestaudio/best\" --merge-output-format mp4", 
                    //TODO Fallback for 265H codec
                    _ => throw new InvalidOperationException("Неизвестный формат: " + _format)
                };

                string outputPath = _outputDirectory != null
                    ? $"--output \"{_outputDirectory}/%(id)s.%(ext)s\" "
                    : string.Empty;

                arguments = $"--quiet {externalArgs}{formatArgs} {outputPath}\"{_url}\"";
                redirectOutput = false;
            }

                return new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp",
                    Arguments = arguments,
                    RedirectStandardOutput = redirectOutput,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            
        }
    } 
}
