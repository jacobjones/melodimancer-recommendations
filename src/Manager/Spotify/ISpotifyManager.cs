using SpotifyAPI.Web;

namespace Melodimancer.Manager.Spotify;

public interface ISpotifyManager
{
    RecommendationsRequest GetRecommendationsRequest(IDictionary<string, float> audioFeatures,
        IEnumerable<string> seedTracks, IEnumerable<string> seedArtists, bool useMinMax,
        double minMaxPercentage);
}