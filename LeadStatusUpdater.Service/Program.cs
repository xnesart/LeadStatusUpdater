using LeadStatusUpdater.Business.Providers;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Settings;
using MassTransit;
using Messaging.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeadStatusUpdater.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.Configure<HttpClientSettings>(context.Configuration.GetSection("HttpClientSettings"));

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