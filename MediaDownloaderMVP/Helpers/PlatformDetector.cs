using MediaDownloaderTgBotMVP.Database.Enums;
using System.Text.RegularExpressions;

namespace MediaDownloaderTgBotMVP.Helpers;

public static class PlatformDetector
{
    private static readonly string tiktokPattern = @"^https?://(www\.|m\.)?(tiktok\.com|vm\.tiktok\.com|vt\.tiktok\.com)";
    private static readonly string youtubePattern = @"^https?://(www\.|m\.|music\.)?(youtube\.com|youtu\.be|youtube-nocookie\.com)";
    public static Platform Detect(string url)
    {
        if (Regex.IsMatch(url, tiktokPattern, RegexOptions.IgnoreCase)) return Platform.TikTok;
        if (Regex.IsMatch(url, youtubePattern, RegexOptions.IgnoreCase)) return Platform.YouTube;
        if (url.Contains("instagram.com")) return Platform.Instagram;
        if (url.Contains("facebook.com") || url.Contains("fb.watch")) return Platform.Facebook;
        if (url.Contains("twitter.com") || url.Contains("x.com")) return Platform.Twitter;
        return Platform.Unknown;
    }
}
