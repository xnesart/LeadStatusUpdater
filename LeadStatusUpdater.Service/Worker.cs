using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Responses;
using MassTransit;
using Messaging.Shared;

namespace LeadStatusUpdater.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IProcessingService _service;
        private readonly IHttpClientService _httpClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private const int BirthdayPeriod = 20;

        public Worker(ILogger<Worker> logger, IProcessingService service, IHttpClientService httpClient,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _service = service;
            _httpClient = httpClient;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

               // var leadsWithBirtday = await ProcessLeadsWithBirtdays(stoppingToken);
               var leadsWithBirtday = new List<LeadDto>();
                var leadsWithTransaction = await ProcessLeadsByTransactionCount(stoppingToken, leadsWithBirtday);

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<List<LeadDto>> ProcessLeadsWithBirtdays(CancellationToken stoppingToken)
        {
            string link = $"https://194.87.210.5:12000/leads-birthdate?{BirthdayPeriod}";

            try
            {
                _logger.LogInformation("Getting leads from httpClient");
                var leads = await _httpClient.Get<List<LeadDto>>(link, stoppingToken);

                _logger.LogInformation("Sending leads into SetLeadsStatusByBirthday");
                var res = _service.SetLeadsStatusByBirthday(leads, BirthdayPeriod);
      
                return res;
                //_logger.LogInformation("Sending leads in Rabbit");
                //await SendUpdateLeadStatusMessage(res);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Published {count} leads with updated status.", res.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing leads by birthday.");
            }

            return new List<LeadDto>();
        }

        private async Task<List<LeadDto>> ProcessLeadsByTransactionCount(CancellationToken stoppingToken,
            List<LeadDto> leadsWithBirthday)
        {
            string link = $"https://194.87.210.5:12000/api/transactions/by-period/15";
        
            try
            {
                _logger.LogInformation("Getting leads from httpClient");
                var leads = await _httpClient.Get<List<TransactionResponse>>(link, stoppingToken);
                
                var res = _service.SetLeadStatusByTransactions(leads,leadsWithBirthday, 30);


                //await SendUpdateLeadStatusMessage(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing leads");
            }

            return new List<LeadDto>();
        }

        private async Task SendUpdateLeadStatusMessage(List<LeadDto> newMessage)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                try
                {
                    await publishEndpoint.Publish(newMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while publishing lead status update message.");
                    throw;
                }
            }
        }
    }
}