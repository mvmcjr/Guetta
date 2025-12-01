using System;
using System.Threading.Tasks;
using Guetta.App;
using Guetta.App.Extensions;
using Guetta.App.Handlers;
using Guetta.Commands.Extensions;
using Guetta.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Guetta
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
            });
            
            builder.Services
                .AddDiscordGateway(options =>
                {
                    options.Token = Environment.GetEnvironmentVariable("TOKEN");
                    options.Intents = GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates | GatewayIntents.Guilds;
                })
                .AddGatewayHandler<ClientReadyHandler>()
                .AddGatewayHandler<MessageCreateHandler>()
                .AddGatewayHandler<VoiceStateUpdateHandler>();

            var serviceCollection = builder.Services;
            serviceCollection.AddOptions();
            serviceCollection.AddGuettaServices();
            serviceCollection.AddGuettaCommands();
            serviceCollection.AddGuettaLocalisation();

            var host = builder.Build();
            
            var ytdlpService = host.Services.GetRequiredService<YoutubeDlService>();
            await ytdlpService.TryUpdate();

            await host.RunAsync();
        }
    }
}