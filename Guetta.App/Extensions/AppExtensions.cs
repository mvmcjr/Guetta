using System.Net.Http.Headers;
using Guetta.App.Spotify;
using Microsoft.Extensions.DependencyInjection;

namespace Guetta.App.Extensions
{
    public static class AppExtensions
    {
        public static void AddGuettaServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CommandSolverService>();
            serviceCollection.AddSingleton<GuildContextManager>();
            serviceCollection.AddTransient<YoutubeDlService>();
            serviceCollection.AddTransient<VideoInformationService>();
            serviceCollection.AddHttpClient<SpotifyService>(c =>
            {
                c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Guetta", "v1"));
            });
        }
    }
}