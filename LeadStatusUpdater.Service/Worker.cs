using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IProcessingService _service;

    public Worker(ILogger<Worker> logger, IProcessingService service)
    {
        _logger = logger;
        _service = service;
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
            _service.GetLeadStatus(response);

            await Task.Delay(1000, stoppingToken);
        }
    }
}