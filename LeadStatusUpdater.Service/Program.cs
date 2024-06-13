using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Settings;

namespace LeadStatusUpdater.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        var configuration = builder.Configuration;
        builder.Services.Configure<HttpClientSettings>(configuration.GetSection("HttpClientSettings"));
        
        builder.Services.AddTransient<IProcessingService, ProcessingService>();
        builder.Services.AddTransient<IHttpClientService, HttpClientService>();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}