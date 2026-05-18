using System.Threading.Channels;
using Telegram.Bot.Types;

namespace MediaDownloaderTgBotMVP;

public record DownloadTask(long ChatId, string Url, Message ProgressMessage);
public class DownloadQueue
{
    private readonly Channel<DownloadTask> _channel =
       Channel.CreateBounded<DownloadTask>(new BoundedChannelOptions(100)
       {
           FullMode = BoundedChannelFullMode.Wait
       });

    public ChannelWriter<DownloadTask> Writer => _channel.Writer;
    public ChannelReader<DownloadTask> Reader => _channel.Reader;
}
