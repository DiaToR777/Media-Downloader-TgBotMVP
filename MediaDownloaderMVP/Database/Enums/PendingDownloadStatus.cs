namespace MediaDownloaderTgBotMVP.Database.Enums;

public enum PendingDownloadStatus
{
    Unknown = 0,
    Analyzing = 1,
    AwaitingChoice = 2,
    Downloading = 3,
    Done = 4,
    Failed = 5
}
