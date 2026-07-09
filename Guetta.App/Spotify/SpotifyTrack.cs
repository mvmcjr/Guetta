using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Guetta.App.Spotify;

internal record SpotifyTrack(
    [property: JsonPropertyName("artists")] IReadOnlyList<Artist> Artists,
    [property: JsonPropertyName("name")] string Name
);