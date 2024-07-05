using LeadStatusUpdater.Business.Providers;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Settings;
using MassTransit;
using Messaging.Shared;

namespace LeadStatusUpdater.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
        
            var configuration = builder.Configuration;
            builder.Services.Configure<HttpClientSettings>(configuration.GetSection("HttpClientSettings"));
            builder.Services.Configure<HttpClientSettings>(configuration.GetSection("HttpClientSettings"));
            
            builder.Services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("rabbitmq://localhost");

                    cfg.Message<LeadListDto>(e => e.SetEntityName("leads-by-birthday"));
                });
            });

            // builder.Services.AddMassTransitHostedService();
            builder.Services.AddTransient<IProcessingService, ProcessingService>();
            builder.Services.AddTransient<IHttpClientService, HttpClientService>();
            builder.Services.AddSingleton<ScopeProvider>();
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}