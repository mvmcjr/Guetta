using System;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.App.Spotify;

namespace Guetta.App;

public class VideoInformationService
{
    public VideoInformationService(YoutubeDlService youtubeDlService, SpotifyService spotifyService)
    {
        YoutubeDlService = youtubeDlService;
        SpotifyService = spotifyService;
    }

    private YoutubeDlService YoutubeDlService { get; }

    private SpotifyService SpotifyService { get; }

    public async Task<PlaylistInformation> GetVideoInformation(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Host.Contains("spotify", StringComparison.InvariantCultureIgnoreCase))
            return await SpotifyService.GetVideoInformation(input);

        return await YoutubeDlService.GetVideoInformation(input);
    }
}