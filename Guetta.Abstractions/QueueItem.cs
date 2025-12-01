namespace Guetta.Abstractions;

public record QueueUser(ulong Id, string Username);

public class QueueItem
{
    public QueueUser User { get; init; }

    public ulong TextChannelId { get; init; }
        
    public ulong VoiceChannelId { get; init; }
        
    public int CurrentQueueIndex { get; set; }

    public bool Playing { get; set; }

    public VideoInformation VideoInformation { get; init; }
}