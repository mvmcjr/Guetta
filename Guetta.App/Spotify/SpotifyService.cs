using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Guetta.Abstractions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;

namespace Guetta.App.Spotify;

public class SpotifyService
{
    public SpotifyService(HttpClient httpClient, YoutubeDlService youtubeDlService, ILogger<SpotifyService> logger)
    {
        HttpClient = httpClient;
        YoutubeDlService = youtubeDlService;
        Logger = logger;
    }

    private HttpClient HttpClient { get; }

    private YoutubeDlService YoutubeDlService { get; }

    private ILogger<SpotifyService> Logger { get; }

    public async Task<PlaylistInformation> GetVideoInformation(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.Host.Contains("spotify", StringComparison.InvariantCultureIgnoreCase))
        {
            var containsTrack = uri.Segments.Any(o => string.Equals(o, "track/", StringComparison.InvariantCultureIgnoreCase));
            var containsPlaylist = uri.Segments.Any(o => string.Equals(o, "playlist/", StringComparison.InvariantCultureIgnoreCase));
            var containsEmbed = uri.Segments.Any(o => string.Equals(o, "embed/", StringComparison.InvariantCultureIgnoreCase));

            Logger.LogInformation("Spotify URL detected {@Parameters}", new
            {
                containsTrack,
                containsPlaylist,
                containsEmbed
            });

            if (!containsTrack && !containsPlaylist)
            {
                Logger.LogWarning("Spotify URL was not a track nor a playlist");
                return null;
            }

            if (!containsEmbed && containsTrack)
            {
                input = $"https://open.spotify.com/embed/track/{uri.Segments.Last()}";
                Logger.LogInformation("Spotify URL was a Track URL but not embedded, URL corrected to: {@NewUrl}", input);
            }

            if (!containsEmbed && containsPlaylist)
            {
                input = $"https://open.spotify.com/embed/playlist/{uri.Segments.Last()}";
                Logger.LogInformation("Spotify URL was a Playlist URL but not embedded, URL corrected to: {@NewUrl}", input);
            }

            return containsTrack ? await YoutubeDlService.GetVideoInformation(ToSearch(await GetSpotifyInformation<SpotifyTrack>(input))) : await GetSpotifyPlaylistInformation(input);
        }

        return null;
    }

    private async Task<PlaylistInformation> GetSpotifyPlaylistInformation(string input)
    {
        var playlist = await GetSpotifyInformation<SpotifyPlaylist>(input);
        var toSearch = playlist.Tracks.Items.Select(i => ToSearch(i.Track)).ToArray();
        var resultsAsync = await toSearch.ToAsyncProcessorBuilder()
            .SelectAsync(async i => await YoutubeDlService.GetVideoInformation(i))
            .ProcessInParallel(20)
            .GetResultsAsync();
        var videos = resultsAsync
            .SelectMany(i => i.Videos)
            .Where(i => !string.IsNullOrEmpty(i.Url))
            .ToArray();
        return new PlaylistInformation
        {
            Title = playlist.Name,
            Videos = videos
        };
    }

    private static string ToSearch(SpotifyTrack data)
    {
        return $"ytsearch:{data.Name} - {data.Artists[0].Name}";
    }

    private async Task<T> GetSpotifyInformation<T>(string url)
    {
        using var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        var resourceNode = doc.DocumentNode.SelectSingleNode("//script[@id='resource']|//script[@id='initial-state']");

        if (resourceNode != null)
        {
            if (resourceNode.InnerText.Contains('{') && resourceNode.InnerText.Contains('}'))
            {
                var json = Uri.UnescapeDataString(resourceNode.InnerText);
                return JsonSerializer.Deserialize<T>(json);
            }

            if (resourceNode.InnerText.StartsWith("ey"))
            {
                var decodedBytes = Convert.FromBase64String(resourceNode.InnerText);
                using var jsonDocument = JsonDocument.Parse(decodedBytes);

                if (jsonDocument.RootElement.TryGetProperty("data", out var dataProperty))
                    return dataProperty.TryGetProperty("entity", out var entityProperty) ? entityProperty.Deserialize<T>() : dataProperty.Deserialize<T>();

                return jsonDocument.RootElement.Deserialize<T>();
            }
        }
        
        throw new InvalidOperationException("Failed to parse spotify response");
    }
}