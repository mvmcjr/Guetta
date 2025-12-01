using System;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.App;
using NetCord.Gateway;
using NetCord.Rest;

namespace Guetta.Commands
{
    internal class VolumeCommand : IDiscordCommand
    {
        public VolumeCommand(GuildContextManager guildContextManager)
        {
            GuildContextManager = guildContextManager;
        }

        private GuildContextManager GuildContextManager { get; }
        
        public async Task ExecuteAsync(Message message, string[] arguments)
        {
            throw new NotImplementedException();
            
            // if (!message.Channel.GuildId.HasValue)
            // {
            //     await message.Channel.SendMessageAsync("Invalid guild ID in channel");
            //     return;
            // }
            //
            // var guildContext = GuildContextManager.GetOrCreate(message.Channel.GuildId.Value);
            // var voice = guildContext.Voice;
            //
            // if (int.TryParse(arguments[0], out var volume) && voice != null)
            // {
            //     await message.Channel.TriggerTypingAsync();
            //     await voice.ChangeVolume(volume / 100f);
            //     await message.Channel.SendMessageAsync("Volume alterado queridão").DeleteMessageAfter(TimeSpan.FromSeconds(5));
            // }
        }
    }
}