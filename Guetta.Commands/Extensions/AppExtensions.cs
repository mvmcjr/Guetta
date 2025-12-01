using Microsoft.Extensions.DependencyInjection;

namespace Guetta.Commands.Extensions
{
    public static class AppExtensions
    {
        public static void AddGuettaCommands(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddCommand<PingCommand>("ping");
            serviceCollection.AddCommand<PlayChannelCommand>("play");
            serviceCollection.AddCommand<SkipChannelCommand>("skip");
            serviceCollection.AddCommand<QueueCommand>("queue");
            serviceCollection.AddCommand<ClearCommand>("clear");
            serviceCollection.AddCommand<ResetCommand>("rs");
        }
    }
}