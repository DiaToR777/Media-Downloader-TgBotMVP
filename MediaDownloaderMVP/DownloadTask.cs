using MediaDownloaderTgBotMVP.Database.Enums;
using Telegram.Bot.Types;

namespace MediaDownloaderTgBotMVP;

public record MessageInfo(int MessageId, long ChatId);
public record DownloadTask
    (
    int DbUserId,
    long ChatId,    
    string Url,
    MessageInfo ProgressMessage,
    int PendingId,
    FileType? ChosenFormat = null
    );
