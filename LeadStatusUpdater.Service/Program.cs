using LeadStatusUpdater.Business.Providers;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Settings;
using MassTransit;
using Messaging.Shared;

namespace LeadStatusUpdater.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var configuration = builder.Configuration;
        builder.Services.Configure<HttpClientSettings>(configuration.GetSection("HttpClientSettings"));

        builder.Services.AddMassTransit(x =>
        {
<<<<<<< HEAD
            var builder = Host.CreateApplicationBuilder(args);
        
            var configuration = builder.Configuration;
            builder.Services.Configure<HttpClientSettings>(configuration.GetSection("HttpClientSettings"));

            builder.Services.AddMassTransit(x =>
=======
            x.UsingRabbitMq((context, cfg) =>
>>>>>>> 791201c5460b528731569d2c9ece15f584d80b16
            {
                cfg.Host("rabbitmq://localhost");

<<<<<<< HEAD
                    cfg.Message<LeadsGuidMessage>(m => { m.SetEntityName("leads-guids-exchange"); });

                    cfg.Publish<LeadsGuidMessage>(p => { p.ExchangeType = "fanout"; });
                });
            });
            
            builder.Services.AddMassTransitHostedService();
            builder.Services.AddTransient<IProcessingService, ProcessingService>();
            builder.Services.AddTransient<IHttpClientService, HttpClientService>();
            builder.Services.AddSingleton<ScopeProvider>();
            builder.Services.AddHostedService<Worker>();
=======
                cfg.Message<LeadsGuidMessage>(m => { m.SetEntityName("leads-guids-exchange"); });

                cfg.Publish<LeadsGuidMessage>(p => { p.ExchangeType = "fanout"; });
            });
        });

        builder.Services.AddTransient<IProcessingService, ProcessingService>();
        builder.Services.AddTransient<IHttpClientService, HttpClientService>();
        builder.Services.AddSingleton<ScopeProvider>();
        builder.Services.AddHostedService<Worker>();
>>>>>>> 791201c5460b528731569d2c9ece15f584d80b16

        var host = builder.Build();
        host.Run();
    }
}