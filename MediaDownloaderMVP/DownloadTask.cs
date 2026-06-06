using MediaDownloaderTgBotMVP.Database.Enums;
using Telegram.Bot.Types;

namespace MediaDownloaderTgBotMVP;

public record DownloadTask
    (
    int DbUserId,
    long ChatId,    
    string Url,
    Message ProgressMessage,
    int PendingId,
    FileType? ChosenFormat = null
    );
