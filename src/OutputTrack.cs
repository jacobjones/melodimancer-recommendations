using SpotifyAPI.Web;

namespace Melodimancer;

public class OutputTrack
{
    public OutputTrack(SimpleTrack simpleTrack, TrackAudioFeatures? features)
    {
        Artists = string.Join(',', simpleTrack.Artists.Select(x => x.Name));
        Name = simpleTrack.Name;
        Href = simpleTrack.Href;
        Uri = simpleTrack.Uri;
        ExternalUrl = simpleTrack.ExternalUrls["spotify"];

        if (features == null)
        {
            return;
        }
        
        Acousticness = features.Acousticness;
        AnalysisUrl = features.AnalysisUrl;
        Danceability = features.Danceability;
        DurationMs = features.DurationMs;
        Energy = features.Energy;
        Id = features.Id;

        Instrumentalness = features.Instrumentalness;
        Key = features.Key;
        Liveness = features.Liveness;
        Loudness = features.Loudness;
        Mode = features.Mode;
        Speechiness = features.Speechiness;
        Tempo = features.Tempo;
        TimeSignature = features.TimeSignature;
        TrackHref = features.TrackHref;
        Type = features.Type;
        Valence = features.Valence;
    }

    public string Artists { get; set; }
    public string Name { get;set; }
    public string Href { get; set; }
    public string Uri { get; set; }
    public string ExternalUrl { get;set; }

    public float Acousticness { get; set; }
    public string AnalysisUrl { get; set; } = default!;
    public float Danceability { get; set; }
    public int DurationMs { get; set; }
    public float Energy { get; set; }
    public string Id { get; set; } = default!;
    public float Instrumentalness { get; set; }
    public int Key { get; set; }
    public float Liveness { get; set; }
    public float Loudness { get; set; }
    public int Mode { get; set; }
    public float Speechiness { get; set; }
    public float Tempo { get; set; }
    public int TimeSignature { get; set; }
    public string TrackHref { get; set; } = default!;
    public string Type { get; set; } = default!;
    public float Valence { get; set; }
}