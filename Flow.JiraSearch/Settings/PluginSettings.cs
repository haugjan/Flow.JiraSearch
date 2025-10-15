namespace Flow.JiraSearch.Settings;

public record PluginSettings
{
    public PluginSettings()
    {
        BaseUrl = "https://www.example.com";
        Timeout = TimeSpan.FromSeconds(10);
        ApiToken = string.Empty;
        MaxResults = 10;
        DefaultProjects = new List<string>();
    }

    public string BaseUrl { get; set; }
    public TimeSpan Timeout { get; set; }
    public string ApiToken { get; set; }
    public List<string> DefaultProjects { get; set; }
    public int MaxResults { get; set; }
}
