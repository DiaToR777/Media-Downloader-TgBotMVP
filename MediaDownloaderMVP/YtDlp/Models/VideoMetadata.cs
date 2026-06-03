using System.Text.Json.Serialization;

namespace MediaDownloaderTgBotMVP.YtDlp.Models;

public class VideoMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("filesize")]
    public long? Filesize { get; set; }

    [JsonPropertyName("filesize_approx")]
    public long? FilesizeApprox { get; set; }

    [JsonPropertyName("extractor_key")]
    public string PlatformName { get; set; } = string.Empty; 

    public long GetFilesize()
    {
        return Filesize ?? FilesizeApprox ?? 0;
    }
}
