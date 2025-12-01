using System;
using System.Collections.Generic;
using Guetta.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Rest;

namespace Guetta.App;

public class GuildContextManager
{
    public GuildContextManager(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    private Dictionary<ulong, GuildContext> ContextByGuild { get; } = new();

    private IServiceProvider ServiceProvider { get; }

    public ICollection<ulong> GetActiveGuilds() => ContextByGuild.Keys;

    private Voice BuildVoice(ulong guildId) => new(
        ServiceProvider.GetRequiredService<YoutubeDlService>(),
        ServiceProvider.GetRequiredService<LocalisationService>(),
        guildId,
        ServiceProvider.GetRequiredService<ILogger<Voice>>(),
        ServiceProvider.GetRequiredService<GatewayClient>(),
        ServiceProvider.GetRequiredService<RestClient>()
    );

    private GuildQueue BuildQueue(ulong guildId, Voice voice) => new(
        ServiceProvider.GetRequiredService<ILogger<GuildQueue>>(),
        voice,
        guildId
    );

    private GuildContext BuildContext(ulong guildId)
    {
        var voice = BuildVoice(guildId);
        var queue = BuildQueue(guildId, voice);
        return new GuildContext(guildId, queue, voice);
    }

    public GuildContext GetOrDefault(ulong guildId)
    {
        return ContextByGuild.GetValueOrDefault(guildId);
    }

    public GuildContext GetOrCreate(ulong guildId)
    {
        if (!ContextByGuild.ContainsKey(guildId))
        {
            ContextByGuild.Add(guildId, BuildContext(guildId));
        }

        return ContextByGuild[guildId];
    }
}