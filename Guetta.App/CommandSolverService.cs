using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.Localisation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;

namespace Guetta.App
{
    public class CommandSolverService
    {
        private ILogger<CommandSolverService> Logger { get; }

        public CommandSolverService(IServiceProvider provider, ILogger<CommandSolverService> logger, IOptions<CommandOptions> options, LocalisationService localisationService, GuildContextManager guildContextManager)
        {
            Provider = provider;
            Logger = logger;
            LocalisationService = localisationService;
            GuildContextManager = guildContextManager;
            CommandOptions = options.Value;
        }

        private CommandOptions CommandOptions { get; }

        private IServiceProvider Provider { get; }
        
        private LocalisationService LocalisationService { get; }
        
        private GuildContextManager GuildContextManager { get; }

        private Task CreateCommandQueueTask(ChannelReader<Message> reader)
        {
            return Task.Run(async () =>
            {
                await foreach (var message in reader.ReadAllAsync())
                {
                    var commandArguments = message.Content[1..].Split(' ');
                    var discordCommand = GetCommand(commandArguments.First().ToLower());

                    if (discordCommand != null)
                        await discordCommand.ExecuteAsync(message, commandArguments.Skip(1).ToArray())
                            .ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                {
                                    Logger.LogError(t.Exception, "Error while running command {@Command}", discordCommand);
                                }
                            });
                    else
                        await LocalisationService.ReplyMessageAsync(message, "InvalidCommand");
                }
            });
        }

        public async ValueTask AddMessageToQueue(Message message)
        {
            if (!message.GuildId.HasValue)
                return;
            
            var guildContext = GuildContextManager.GetOrCreate(message.GuildId.Value);
            guildContext.CommandChannelTask ??= CreateCommandQueueTask(guildContext.CommandChannel);
            
            await guildContext.CommandChannel.Writer.WriteAsync(message);
        }

        private IDiscordCommand GetCommand(string command)
        {
            Logger.LogInformation("Command received: {@Command}", command);

            if (CommandOptions.Commands.TryGetValue(command, out var commandType))
            {
                Logger.LogDebug("Command {@Command} solved to type {@CommandType}", command, commandType.Name);

                return (IDiscordCommand)Provider.GetService(commandType);
            }

            return null;
        }
    }
}