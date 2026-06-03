
using MediaDownloaderTgBotMVP.Database.Enums;

namespace MediaDownloaderTgBotMVP.Helpers;

public readonly struct ParseResult
{
    public Platform Platform { get; }
    public string? VideoId { get; }
    public bool IsShortUrl { get; }

    public ParseResult(Platform platform, string? videoId, bool isShortUrl)
    {
        Platform = platform;
        VideoId = videoId;
        IsShortUrl = isShortUrl;
    }
}
