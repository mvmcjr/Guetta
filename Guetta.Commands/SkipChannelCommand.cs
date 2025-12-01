using System;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.App;
using Guetta.App.Extensions;
using Guetta.Localisation;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Rest;

namespace Guetta.Commands
{
    internal class SkipChannelCommand(
        LocalisationService localisationService,
        GuildContextManager guildQueue,
        ILogger<SkipChannelCommand> logger)
        : IDiscordCommand
    {
        public async Task ExecuteAsync(Message message, string[] arguments)
        {
            if (!message.GuildId.HasValue)
            {
                if (message.Channel != null)
                    await message.Channel.SendMessageAsync("Invalid guild ID in channel");
                else
                    logger.LogWarning("Message channel is null in SkipChannelCommand");
                
                return;
            }

            var guildContext = guildQueue.GetOrCreate(message.GuildId.Value);
            var queue = guildContext.GuildQueue;

            if (!queue.CanSkip())
            {
                await localisationService.SendMessageAsync(message.ChannelId, "CantSkip", message.Author.Username)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(15));
                return;
            }

            queue.Skip();
            await localisationService.SendMessageAsync(message.ChannelId, "SongSkipped", message.Author.Username)
                .DeleteMessageAfter(TimeSpan.FromSeconds(15));
        }
    }
}