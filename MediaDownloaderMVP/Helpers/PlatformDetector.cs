using MediaDownloaderTgBotMVP.Database.Enums;
using System.Text.RegularExpressions;

namespace MediaDownloaderTgBotMVP.Helpers;

public static class PlatformDetector
{
    private static readonly Regex TiktokRegex = new(
                @"^(?:https?:\/\/)?(?:www\.|m\.)?(?:vm|vt)\.tiktok\.com\/([\w-]+)|(?:www\.)?tiktok\.com\/@[\w.-]+\/video\/(\d+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex YoutubeRegex = new(
            @"^(?:https?:\/\/)?(?:www\.|m\.|music\.)?(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=|shorts\/|youtube-nocookie\.com\/))([\w-]{11})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex InstagramRegex = new(@"instagram\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FacebookRegex = new(@"facebook\.com|fb\.watch", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TwitterRegex = new(@"twitter\.com|x\.com", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static (Platform Platform, string? VideoId) Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (Platform.Unknown, null);

        var ytMatch = YoutubeRegex.Match(url);
        if (ytMatch.Success)
        {
            return (Platform.YouTube, ytMatch.Groups[1].Value);
        }

        var ttMatch = TiktokRegex.Match(url);
        if (ttMatch.Success)
        {
            string? videoId = !string.IsNullOrEmpty(ttMatch.Groups[2].Value)
                ? ttMatch.Groups[2].Value
                : (!string.IsNullOrEmpty(ttMatch.Groups[1].Value) ? ttMatch.Groups[1].Value : null);

            return (Platform.TikTok, videoId);
        }

        if (InstagramRegex.IsMatch(url)) return (Platform.Instagram, null);
        if (FacebookRegex.IsMatch(url)) return (Platform.Facebook, null);
        if (TwitterRegex.IsMatch(url)) return (Platform.Twitter, null);

        return (Platform.Unknown, null);
    }
}
