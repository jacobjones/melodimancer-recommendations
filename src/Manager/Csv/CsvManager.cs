using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Configuration;

namespace Melodimancer.Manager.Csv;

public class CsvManager : ICsvManager
{
    private readonly IConfiguration _configuration;

    public CsvManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public IList<FileInfo> GetCsvFileInfos()
    {
        var pathSettings = _configuration.GetRequiredSection("Path").Get<PathSettings>();

        if (pathSettings == null)
        {
            return new List<FileInfo>();
        }

        var csvDirectoryInfo = new DirectoryInfo(pathSettings.CsvPath);

        return csvDirectoryInfo.GetFiles("*.csv")
            .OrderBy(x => x.Name).ToList();
    }

    public async Task<IDictionary<string, IDictionary<string, float?>>> GetPersonalAudioFeaturesFromCsvAsync(string[] columns, string csvPath)
    {
        const string variableColumnName = "Variable";
        
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        await csv.ReadAsync();
        csv.ReadHeader();

        var headerRecord = csv.HeaderRecord;

        if (headerRecord == null || !headerRecord.Contains(variableColumnName))
        {
            throw new Exception($"No column found with name '${variableColumnName}'");
        }

        var importColumns = headerRecord.Where(columns.Contains).ToList();

        if (!importColumns.Any())
        {
            throw new Exception($"No variables found in the CSV.");
        }
        
        IDictionary<string, IDictionary<string, float?>> variables = new Dictionary<string, IDictionary<string, float?>>();

        while (await csv.ReadAsync())
        {
            var variable = csv.GetField<string>(variableColumnName);

            IDictionary<string, float?> values = importColumns.ToDictionary(x => x, x => csv.GetField<float?>(x));

            variables.Add(variable!, values);
        }

        return variables;
    }
}