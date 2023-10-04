using System.Globalization;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

namespace Melodimancer.Manager.Spotify;

public class SpotifyManager : ISpotifyManager
{
    private readonly IConfiguration _configuration;

    public SpotifyManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public RecommendationsRequest GetRecommendationsRequest(IDictionary<string, float> audioFeatures, IEnumerable<string> seedTracks, IEnumerable<string> seedArtists, bool useMinMax, double minMaxPercentage)
    {
        var spotifySettings = _configuration.GetRequiredSection("Spotify").Get<SpotifySettings>();

        var request = new RecommendationsRequest();
        
        foreach (var audioFeature in audioFeatures)
        {
            if (useMinMax)
            {
                var percentage = Math.Abs(audioFeature.Value * minMaxPercentage);
                
                request.Min.Add(audioFeature.Key.ToLowerInvariant(), (audioFeature.Value - percentage).ToString(CultureInfo.InvariantCulture));
                request.Max.Add(audioFeature.Key.ToLowerInvariant(), (audioFeature.Value + percentage).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                request.Target.Add(audioFeature.Key.ToLowerInvariant(), audioFeature.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
        
        foreach (var seedTrack in seedTracks)
        {
            request.SeedTracks.Add(seedTrack);
        }
        
        foreach (var seedArtist in seedArtists)
        {
            request.SeedArtists.Add(seedArtist);
        }

        request.Limit = spotifySettings.Limit;

        return request;
    }
}