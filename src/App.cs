using System.Text.RegularExpressions;
using ArrayToExcel;
using ConsoleTables;
using Melodimancer.Manager.Csv;
using Melodimancer.Manager.Spotify;
using Microsoft.Extensions.Configuration;
using SpotifyAPI.Web;

namespace Melodimancer;

public class App
{
    private readonly IConfiguration _configuration;
    private readonly ICsvManager _csvManager;
    private readonly ISpotifyManager _spotifyManager;

    private const double MinMaxPercentage = 0.3;
    private const int MaxSeeds = 5;
    private const string TrackUrl = "https://open.spotify.com/track/";
    private const string ArtistUrl = "https://open.spotify.com/artist/";
    
    // All the columns which we care about importing
    private readonly string[] _columns = { "Q1", "Q3", "V2", "V3", "V4" };
    
    public App(IConfiguration configuration, ICsvManager csvManager, ISpotifyManager spotifyManager)
    {
        _configuration = configuration;
        _csvManager = csvManager;
        _spotifyManager = spotifyManager;
    }

    public async Task Run()
    {
        var csvPath = SelectCsv(_csvManager.GetCsvFileInfos());

        if (csvPath == null)
        {
            return;
        }

        IDictionary<string, IDictionary<string, float?>> audioFeatures2;
        
        try
        {
            audioFeatures2 = await _csvManager.GetPersonalAudioFeaturesFromCsvAsync(_columns, csvPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        Console.WriteLine();
        DisplayCsv(audioFeatures2);
        Console.WriteLine();

        var column = SelectColumn(audioFeatures2.First().Value.Keys.ToArray());

        var selectedFeatures = SelectFeatures(column, audioFeatures2);
        var useMinMax = UseMinMax();

        var seedTracks = GetSeeds("track", TrackUrl);
        var seedArtists = GetSeeds("artist", ArtistUrl);
        
        // Load our Spotify settings
        var spotifySettings = _configuration.GetRequiredSection("Spotify").Get<SpotifySettings>();
        
        var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new ClientCredentialsAuthenticator(spotifySettings.ClientId, spotifySettings.ClientSecret));
        var client = new SpotifyClient(config);

        var request = _spotifyManager.GetRecommendationsRequest(selectedFeatures, seedTracks, seedArtists, useMinMax, MinMaxPercentage);

        var recommendations = await client.Browse.GetRecommendations(request);
        
        var trackFeatures =
            await client.Tracks.GetSeveralAudioFeatures(
                new TracksAudioFeaturesRequest(recommendations.Tracks.Select(x => x.Id).ToList()));


        var outputTracks = recommendations.Tracks.Select(t =>
            new OutputTrack(t, trackFeatures.AudioFeatures.SingleOrDefault(f => f.Id == t.Id))).ToList();

        var pathSettings = _configuration.GetRequiredSection("Path").Get<PathSettings>();

        IList<RequestItem> requestData = new List<RequestItem>
        {
            new("Seed Tracks", string.Join(',', request.SeedTracks)),
            new("Seed Artists", string.Join(',', request.SeedArtists)),
            new("Limit", string.Join(',', request.Limit))
        };

        foreach (var target in request.Target)
        {
            requestData.Add(new RequestItem($"target_{target.Key}", target.Value));
        }
        
        foreach (var target in request.Min)
        {
            requestData.Add(new RequestItem($"min_{target.Key}", target.Value));
        }
        
        foreach (var target in request.Max)
        {
            requestData.Add(new RequestItem($"max_{target.Key}", target.Value));
        }

        var excel = outputTracks.ToExcel(x => x.SheetName("Tracks").AddSheet(requestData));

        var filename = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{outputTracks.Count}_Tracks.xlsx".ToLower();
        var path = $"{pathSettings.OutputPath}/{filename}";

        var file = new FileInfo(path);
        file.Directory?.Create();
        await File.WriteAllBytesAsync(path, excel);
        
        Console.WriteLine($"Output saved to {path}.");
    }

    private static IDictionary<string, float> SelectFeatures(string column, IDictionary<string, IDictionary<string, float?>> audioFeatures)
    {
        // For Vs return everything that is not null
        if (column.StartsWith('V'))
        {
            return audioFeatures.Where(x => x.Value[column].HasValue)
                .ToDictionary(x => x.Key, x => x.Value[column]!.Value);
        }
        
        var checkbox = new Checkbox("Select the audio features.", true, true,
            audioFeatures.Where(x => x.Value[column].HasValue).Select(x => $"{x.Key} ({x.Value[column]})").ToArray());

        var featureOptions = checkbox.Select().Select(x => x.Option.Split(' ').First()).ToList();

        return audioFeatures.Where(x => featureOptions.Contains(x.Key))
            .ToDictionary(x => x.Key, x => x.Value[column]!.Value);
    }
    
    private static bool UseMinMax()
    {
        var columnCheckbox = new Checkbox($"Select target or min/max.", false, true,
            $"Min/Max (± {MinMaxPercentage * 100}%)", "Target");
        
        var columnResponse = columnCheckbox.Select();

        return columnResponse.Single().Index == 0;
    }
    
    private static string SelectColumn(string[] columns)
    {
        var columnCheckbox = new Checkbox($"Select the column.", false, true,
            columns);
        
        var columnResponse = columnCheckbox.Select();

        return columnResponse.Single().Option;
    }
    
    private static string? SelectCsv(IList<FileInfo> csvPaths)
    {
        if (!csvPaths.Any())
        {
            Console.WriteLine("No CSV found at specified path.");
            return null;
        }
        
        if (csvPaths.Count == 1)
        {
            Console.WriteLine($"Using CSV: {csvPaths.First().Name}.");
            return csvPaths.First().FullName;
        }

        var csvCheckbox = new Checkbox($"Select the target CSV file.", false, true,
            csvPaths.Select(x => x.Name).ToArray());
        
        var csvResponse = csvCheckbox.Select();

        return csvPaths[csvResponse.Single().Index].FullName;
    }

    private static void DisplayCsv(IDictionary<string, IDictionary<string, float?>> audioFeatures)
    {
        var columnNames = audioFeatures.First().Value.Keys.ToList();
        columnNames.Insert(0, "Variable");
        
        var table = new ConsoleTable(columnNames.ToArray());
        
        foreach (var audioFeature in audioFeatures)
        {
            IList<string?> row = audioFeature.Value.Values.Select(x => x?.ToString("n3") ?? null).ToList();
            row.Insert(0, audioFeature.Key);
            
            table.AddRow(row.Cast<object>().ToArray());
        }
        
        table.Write(Format.Minimal);
        Console.WriteLine("Press any key to continue");
      
        Console.ReadKey();
    }

    private static IList<string> GetSeeds(string type, string url)
    {
        Console.WriteLine($"Enter up to {MaxSeeds} seed {type}(s) (ID or {type} URL):");
        
        IList<string> seedTracks = new List<string>();
        
        for(var i = 0; i < MaxSeeds; i++)
        {
            var answer = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(answer))
            {
                break;
            }

            if (!(answer.StartsWith(url) || Regex.IsMatch(answer, "^[a-zA-Z0-9]{22}$")))
            {
                Console.WriteLine("Not valid");
                continue;
            }
            
            seedTracks.Add(answer.Replace(url,"").Split('?').First());
        }

        return seedTracks;
    }
}