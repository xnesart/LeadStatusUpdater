using LeadStatusUpdater.Business.Providers;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Settings;
using MassTransit;
using Messaging.Shared;
using Serilog;

namespace LeadStatusUpdater.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args).UseSerilog() 
                .ConfigureServices((context, services) =>
                {
                    services.Configure<HttpClientSettings>(context.Configuration.GetSection("HttpClientSettings"));
                    services.Configure<LeadProcessingSettings>(context.Configuration.GetSection("ConfigurationMessage"));

                    services.AddMassTransit(x =>
                    {
                        x.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host("rabbitmq://localhost");

                            cfg.Message<LeadsGuidMessage>(m =>
                            {
                                m.SetEntityName("leads-guids-exchange");
                            });

                            cfg.Publish<LeadsGuidMessage>(p =>
                            {
                                p.ExchangeType = "fanout";
                            });
                        });
                    });

                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .CreateLogger();
                    
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
}