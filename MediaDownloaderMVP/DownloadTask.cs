using Telegram.Bot.Types;

namespace MediaDownloaderTgBotMVP;

public record DownloadTask(long ChatId, string Url, Message ProgressMessage);
