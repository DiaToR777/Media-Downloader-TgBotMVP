using MediaDownloaderTgBotMVP.Database.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaDownloaderTgBotMVP.Helpers
{
    public class LinkParser
    {
        private static readonly Regex YoutubeRegex = new(
        @"^(?:https?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=|shorts\/))([\w-]{11})(?:\S+)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TikTokRegex = new(
        @"^(?:https?:\/\/)?(?:vm|vt)\.tiktok\.com\/([\w-]+)|(?:www\.)?tiktok\.com\/@[\w.-]+\/video\/(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public (Platform Platform, string VideoId)? Parse(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            // 1. Проверяем YouTube
            var ytMatch = YoutubeRegex.Match(url);
            if (ytMatch.Success)
            {
                return (Platform.YouTube, ytMatch.Groups[1].Value);
            }

            // 2. Проверяем TikTok
            var ttMatch = TikTokRegex.Match(url);
            if (ttMatch.Success)
            {
                // Если в группе 2 есть цифры — это прямой ID видео (полная ссылка)
                if (!string.IsNullOrEmpty(ttMatch.Groups[2].Value))
                {
                    return (Platform.TikTok, ttMatch.Groups[2].Value);
                }

                // Если цифр нет, значит ссылка короткая (vm.tiktok.com/XYZ/). 
                // Берем буквенный токен XYZ из группы 1 как временный ID для кэша.
                if (!string.IsNullOrEmpty(ttMatch.Groups[1].Value))
                {
                    return (Platform.TikTok, ttMatch.Groups[1].Value);
                }
            }

            return null; // Ссылка не подходит под поддерживаемые платформы
        }
    }
}
