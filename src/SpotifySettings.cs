namespace Melodimancer;

public class SpotifySettings
{
    public string ClientId { get; set; }
    
    public string ClientSecret { get; set; }

    public int Limit { get; set; } = 100;
}