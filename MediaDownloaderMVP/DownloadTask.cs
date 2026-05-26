using Telegram.Bot.Types;
using MediaDownloaderTgBotMVP.Database.Enums;

namespace MediaDownloaderTgBotMVP;

public record DownloadTask
    (
    int DbUserId,
    long ChatId,    
    string Url,
    Message ProgressMessage,
    Platform Platform
    );
