using MediaDownloaderTgBotMVP.Database.Enums;
using System.Text.RegularExpressions;

namespace MediaDownloaderTgBotMVP.Helpers;

public static class PlatformDetector
{
    private static readonly Regex TikTokLongRegex = new(@"/video/(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TikTokShortPathRegex = new(@"/t/([a-zA-Z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex YouTubeStandardRegex = new(@"[?&]v=([^&?#]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex YouTubeShortsRegex = new(@"/shorts/([^/?#]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex YouTubeLiveRegex = new(@"/live/([^/?#]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static ParseResult Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new ParseResult(Platform.Unknown, null, false);

        string lowerUrl = url.ToLowerInvariant();

        if (lowerUrl.Contains("tiktok.com") || lowerUrl.Contains("douyin.com"))
        {
            if (lowerUrl.Contains("vt.tiktok.com") ||
                lowerUrl.Contains("vm.tiktok.com") ||
                lowerUrl.Contains("v.douyin.com"))
            {
                return new ParseResult(Platform.TikTok, null, isShortUrl: true);
            }

            if (TikTokShortPathRegex.IsMatch(url))
            {
                return new ParseResult(Platform.TikTok, null, isShortUrl: true);
            }

            var longMatch = TikTokLongRegex.Match(url);
            if (longMatch.Success)
            {
                return new ParseResult(Platform.TikTok, longMatch.Groups[1].Value, isShortUrl: false);
            }

            return new ParseResult(Platform.TikTok, null, isShortUrl: false);
        }

        if (lowerUrl.Contains("youtube.com") || lowerUrl.Contains("youtu.be"))
        {
            if (lowerUrl.Contains("youtu.be"))
            {
                return new ParseResult(Platform.YouTube, null, isShortUrl: true);
            }

            var stdMatch = YouTubeStandardRegex.Match(url);
            if (stdMatch.Success)
            {
                return new ParseResult(Platform.YouTube, stdMatch.Groups[1].Value, isShortUrl: false);
            }

            var shortsMatch = YouTubeShortsRegex.Match(url);
            if (shortsMatch.Success)
            {
                return new ParseResult(Platform.YouTube, shortsMatch.Groups[1].Value, isShortUrl: false);
            }

            var liveMatch = YouTubeLiveRegex.Match(url);
            if (liveMatch.Success)
            {
                return new ParseResult(Platform.YouTube, liveMatch.Groups[1].Value, isShortUrl: false);
            }

            return new ParseResult(Platform.YouTube, null, isShortUrl: false);
        }

        return new ParseResult(Platform.Unknown, null, false);
        //TODO another platforms
    }

}
