using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IProcessingService _service;
    private readonly IHttpClientService _httpClient;

    public Worker(ILogger<Worker> logger, IProcessingService service, IHttpClientService httpClient)
    {
        _logger = logger;
        _service = service;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            GetLeadsResponse response = new GetLeadsResponse();
            var leads = _httpClient.Get<GetLeadsResponse>("someUrl", stoppingToken);
            _service.GetLeadStatus(response);

            await Task.Delay(1000, stoppingToken);
        }
    }
}