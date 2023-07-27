namespace Melodimancer.Manager.Csv;

public interface ICsvManager
{
    IList<FileInfo> GetCsvFileInfos();

    Task<IDictionary<string, IDictionary<string, float?>>> GetPersonalAudioFeaturesFromCsvAsync(string[] columns, string csvPath);
}