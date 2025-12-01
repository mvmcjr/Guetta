using System.Threading.Channels;
using System.Threading.Tasks;
using NetCord.Gateway;

namespace Guetta.App;

public class GuildContext
{
    public ulong Id { get; }

    public GuildQueue GuildQueue { get; }

    public Voice Voice { get; }

    internal Channel<Message> CommandChannel { get; } = Channel.CreateBounded<Message>(100);
    
    internal Task CommandChannelTask { get; set; }

    public GuildContext(ulong id, GuildQueue guildQueue, Voice voice)
    {
        Id = id;
        GuildQueue = guildQueue;
        Voice = voice;
    }
}