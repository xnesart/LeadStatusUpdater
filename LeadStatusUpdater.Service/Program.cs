using LeadStatusUpdater.Business.Services;

namespace LeadStatusUpdater.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddTransient<IProcessingService, ProcessingService>();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.Run();
    }
}