namespace Melodimancer;

public class RequestItem
{
    public RequestItem(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    
    public string Value { get; set; }
}