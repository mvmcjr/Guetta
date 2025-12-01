using System;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.App;
using Guetta.App.Extensions;
using Guetta.Localisation;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace Guetta.Commands;

public class ResetCommand(GuildContextManager guildContextManager, LocalisationService localisationService, ILogger<ResetCommand> logger)
    : IDiscordCommand
{
    private GuildContextManager GuildContextManager { get; } = guildContextManager;

    private LocalisationService LocalisationService { get; } = localisationService;

    public async Task ExecuteAsync(Message message, string[] arguments)
    {
        if (!message.GuildId.HasValue)
        {
            if (message.Channel != null)
                await message.Channel.SendMessageAsync("Invalid guild ID in channel");
            else
                logger.LogWarning("Message channel is null in ResetCommand");
                
            return;
        }
        
        var guildContext = GuildContextManager.GetOrCreate(message.GuildId.Value);
        
        if (guildContext.GuildQueue.Count <= 0)
        {
            await LocalisationService
                .SendMessageAsync(message.ChannelId, "NoSongsInQueue")
                .DeleteMessageAfter(TimeSpan.FromSeconds(10));
        }
        else
        {
            guildContext.GuildQueue.Clear();
        
            await LocalisationService
                .SendMessageAsync(message.ChannelId, "QueueCleared")
                .DeleteMessageAfter(TimeSpan.FromSeconds(10));
        }
    }
}