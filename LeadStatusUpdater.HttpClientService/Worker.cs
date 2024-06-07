using System.Text.Json;
using LeadStatusUpdater.Core.Responses;
using RestSharp;
using RestSharp.Authenticators;

namespace LeadStatusUpdater.HttpClientService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private const string GetLeadsUrl = "https://194.87.210.5:11000/api/";

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                var list= GetLeads(stoppingToken);

                Console.WriteLine();
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    public async Task<GetLeadsResponse> GetLeads(CancellationToken cancellationToken)
    {
        var options = new RestClientOptions(GetLeadsUrl);
    
        var client = new RestClient(options);

        var request = new RestRequest("report/leads-with-transactions");
        
        _logger.LogInformation("Getting response at: {time}", DateTimeOffset.Now);
        var response = await client.GetAsync<GetLeadsResponse>(request, cancellationToken);
        
        return response;
    }
}