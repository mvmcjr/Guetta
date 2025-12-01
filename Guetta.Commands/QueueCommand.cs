using System;
using System.Linq;
using System.Text;
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
    internal class QueueCommand(LocalisationService localisationService, GuildContextManager guildContextManager, ILogger<QueueCommand> logger)
        : IDiscordCommand
    {
        private GuildContextManager GuildContextManager { get; } = guildContextManager;

        private LocalisationService LocalisationService { get; } = localisationService;

        private const int Limit = 10;
        
        public async Task ExecuteAsync(Message message, string[] arguments)
        {
            if (!message.GuildId.HasValue)
            {
                if (message.Channel != null)
                    await message.Channel.SendMessageAsync("Invalid guild ID in channel");
                else
                    logger.LogWarning("Message channel is null in QueueCommand");
                
                return;
            }
            
            var guildContext = GuildContextManager.GetOrCreate(message.GuildId.Value);
            
            var queueMessageBuilder = new StringBuilder();
            var template = LocalisationService.GetMessageTemplate("QueueItem");
            var queueItems = guildContext.GuildQueue.GetQueueItems().ToArray();
            
            foreach (var queueItem in queueItems.Take(Limit))
            {
                queueMessageBuilder.AppendLine(string.Format(template, queueItem.CurrentQueueIndex + 1, queueItem.VideoInformation.Title, queueItem.User.Username));
            }
            
            if (queueItems.Length > Limit)
            {
                queueMessageBuilder.AppendLine("And more...");
            }
            
            var queueMessage = queueMessageBuilder.ToString();
            if (string.IsNullOrEmpty(queueMessage))
            {
                await LocalisationService.SendMessageAsync(message.ChannelId, "NoSongsInQueue", message.Author.Username)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
            
                return;
            }

            if (message.Channel != null)
                await message.Channel
                    .SendMessageAsync(queueMessage)
                    .DeleteMessageAfter(TimeSpan.FromMinutes(1));
        }
    }
}