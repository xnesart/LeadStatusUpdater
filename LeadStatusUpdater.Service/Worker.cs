using System.Text.Json;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Responses;
using MassTransit;

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

                var leadsWithBirtday = await ProcessLeadsWithBirtdays(stoppingToken);
              
                var leadsWithTransaction = await ProcessLeadsByTransactionCount(stoppingToken, 15);

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<List<Guid>> ProcessLeadsWithBirtdays(CancellationToken stoppingToken)
        {
            //брать из настроек
            string link = $"https://194.87.210.5:12000/leads-birthdate?periodBdate={BirthdayPeriod}";

            try
            {
                _logger.LogInformation("Getting leads from httpClient");
                var leads = await _httpClient.Get<List<LeadDto>>(link, stoppingToken);

                var result = leads.Select(l => l.Id).ToList();
                //_logger.LogInformation("Sending leads into SetLeadsStatusByBirthday");
                //var res = _service.SetLeadsStatusByBirthday(leads, BirthdayPeriod);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing leads by birthday.");
            }

            return new List<Guid>();
        }

        private async Task<List<Guid>> ProcessLeadsByTransactionCount(CancellationToken stoppingToken,
            int byDays)
        {
            //брать из настроек
            string link = $"https://194.87.210.5:12000/api/transactions/by-period/{byDays}";

            try
            {
                _logger.LogInformation("Getting transactions from httpClient");
                var transactions = await _httpClient.Get<List<TransactionResponse>>(link, stoppingToken);

                _logger.LogInformation("Processing transactions to update lead statuses");
                var res = _service.SetLeadStatusByTransactions(transactions);

                return res;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while processing transactions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing leads");
            }

            return new List<Guid>();
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