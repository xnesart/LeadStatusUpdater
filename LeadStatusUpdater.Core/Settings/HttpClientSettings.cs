namespace LeadStatusUpdater.Core.Settings;

public class HttpClientSettings
{
    public string BaseUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 380;
}