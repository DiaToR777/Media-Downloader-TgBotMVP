using Telegram.Bot.Types;
using MediaDownloaderTgBotMVP.Database.Enums;

namespace MediaDownloaderTgBotMVP;

public record DownloadTask(long ChatId, string Url, Message ProgressMessage, Platform Platform);
