using System;
using System.Linq;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Rest;

namespace Guetta.Commands
{
    internal class ClearCommand(RestClient discordSocketClient, IOptions<CommandOptions> commandOptions)
        : IDiscordCommand
    {
        private RestClient DiscordSocketClient { get; } = discordSocketClient;

        private IOptions<CommandOptions> CommandOptions { get; } = commandOptions;

        public async Task ExecuteAsync(Message message, string[] arguments)
        {
            var timeLimit = DateTime.Now.AddDays(-14);
            if (message.Channel != null)
            {
                var currentUser = await DiscordSocketClient.GetCurrentUserAsync();

                var messages = await message.Channel.GetMessagesAsync().ToListAsync();
                var messagesToDelete = messages
                    .Where(chatMessage => (chatMessage.Author.Id == currentUser.Id || chatMessage.Content.StartsWith(CommandOptions.Value.Prefix)) && chatMessage.CreatedAt >= timeLimit)
                    .Select(x => x.Id)
                    .ToList();
            
                await message.Channel.DeleteMessagesAsync(messagesToDelete);
            }
        }
    }
}