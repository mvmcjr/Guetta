using System;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.App.Extensions;
using NetCord.Gateway;

namespace Guetta.Commands
{
    internal class PingCommand : IDiscordCommand
    {
        public async Task ExecuteAsync(Message message, string[] arguments)
        {
            if (message.Channel != null)
                await message.Channel.SendMessageAsync($"{message.Author.Username} pong")
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
        }
    }
}