using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Guetta.App.Handlers;

public class VoiceStateUpdateHandler(GuildContextManager guildContextManager, RestClient restClient, GatewayClient gatewayClient) : IVoiceStateUpdateGatewayHandler
{
    public async ValueTask HandleAsync(VoiceState e)
    {
        if (e.ChannelId == null)
        {
            var guildContext = guildContextManager.GetOrDefault(e.GuildId);
            var currentVoiceState = await restClient.GetCurrentGuildUserVoiceStateAsync(e.GuildId);

            if (guildContext?.Voice.AudioChannelId != null && currentVoiceState.ChannelId != null)
            {
                var cacheGuild = gatewayClient.Cache.Guilds[e.GuildId];
                var voiceStates =
                    cacheGuild.VoiceStates.Values.Where(x => x.ChannelId == currentVoiceState.ChannelId.Value && x.User != null);

                var allBots = voiceStates.All(x => x.User != null && x.User.IsBot);
                
                if (allBots)
                {
                    guildContext.GuildQueue.Clear();
                }
            }
        }
    }
}

public class ClientReadyHandler(ILogger<ClientReadyHandler> logger) : IReadyGatewayHandler
{
    public ValueTask HandleAsync(ReadyEventArgs arg)
    {
        logger.LogInformation("Ready to go!");
        return ValueTask.CompletedTask;
    }
}