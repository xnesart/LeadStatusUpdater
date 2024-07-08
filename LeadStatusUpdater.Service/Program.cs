using LeadStatusUpdater.Business.Providers;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Settings;
using MassTransit;
using Messaging.Shared;
using Serilog;

namespace LeadStatusUpdater.Service;

public class Program
{
    public static void Main(string[] args)
    {
        IConfiguration configuration;

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true);

        configuration = builder.Build();

        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog((context, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console())
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(configuration);

                services.Configure<HttpClientSettings>(context.Configuration.GetSection("HttpClientSettings"));
                services.Configure<LeadProcessingSettings>(context.Configuration.GetSection("ConfigurationMessage"));

                services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host("rabbitmq://localhost");

                        cfg.Message<LeadsGuidMessage>(m => { m.SetEntityName("leads-guids-exchange"); });

                        cfg.Publish<LeadsGuidMessage>(p => { p.ExchangeType = "fanout"; });
                    });
                });

                services.AddMassTransitHostedService();

                services.AddTransient<IProcessingService, ProcessingService>();
                services.AddTransient<IHttpClientService, HttpClientService>();
                services.AddSingleton<ScopeProvider>();
                services.AddHostedService<Worker>();
            })
            .Build();

        host.Run();
    }
}