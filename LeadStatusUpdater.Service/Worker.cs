using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Responses;
using LeadStatusUpdater.Core.Settings;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Messaging.Shared;
using Newtonsoft.Json.Linq;

namespace LeadStatusUpdater.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IProcessingService _service;
        private readonly IHttpClientService _httpClient;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private Timer _timer;
        private int _countOfStarts;

        public Worker(ILogger<Worker> logger, IProcessingService service, IHttpClientService httpClient,
            IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _service = service;
            _httpClient = httpClient;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting at: {time}", DateTimeOffset.Now);
            if (_countOfStarts == 0)
            {
                _logger.LogInformation("Initial start detected, executing DoWork immediately.");
                DoWork(cancellationToken);
            }

            _countOfStarts++;

            var now = DateTime.UtcNow;
            var moscowTime = now.AddHours(3);
            _logger.LogInformation("Current UTC time: {utcTime}, Moscow time: {moscowTime}", now, moscowTime);

            var scheduledTime = new DateTime(moscowTime.Year, moscowTime.Month, moscowTime.Day, 2, 0, 0);
            _logger.LogInformation("Scheduled time for first run: {scheduledTime}", scheduledTime);

            if (moscowTime.Hour >= 2)
            {
                scheduledTime = scheduledTime.AddDays(1);
                _logger.LogInformation("Current time is after 02:00, rescheduling to: {scheduledTime}", scheduledTime);
            }

            var dueTime = scheduledTime - moscowTime;
            _logger.LogInformation("Time until next scheduled run: {dueTime}", dueTime);

            _timer = new Timer(state => DoWork(cancellationToken), null, dueTime, TimeSpan.FromDays(1));

            return base.StartAsync(cancellationToken);
        }

        private void DoWork(CancellationToken cancellationToken)
        {
            try
            {
                UpdateAppSettingsAsync(cancellationToken);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var leadsWithBirthday = ProcessLeadsWithBirthdays(cancellationToken).GetAwaiter().GetResult();
                _logger.LogInformation("Processed leads with birthdays: {count}", leadsWithBirthday.Count);

                var leadsWithTransaction = ProcessLeadsByTransactionCount(cancellationToken).GetAwaiter().GetResult();
                _logger.LogInformation("Processed leads by transaction count: {count}", leadsWithTransaction.Count);

                var message = new LeadsMessage
                {
                    Leads = leadsWithBirthday.Union(leadsWithTransaction).ToList()
                };

                _logger.LogInformation("Prepared LeadsMessage with total leads: {count}", message.Leads.Count);
                SendUpdateLeadStatusMessage(message).GetAwaiter().GetResult();
                _logger.LogInformation("Message sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background worker.");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExecuteAsync called, but logic is handled in StartAsync and DoWork.");
            await Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }

        private async Task UpdateAppSettingsAsync(CancellationToken stoppingToken)
        {
            try
            {
                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                var appSettings = await GetAppSettings(stoppingToken);
                var baseUrlForSettings = appSettings["HttpClientSettings"]["UrlForSettings"].ToString();
        
                var configurationMessage =
                    await _httpClient.Get<Dictionary<string, string>>(baseUrlForSettings, stoppingToken);
        
                foreach (var kvp in configurationMessage)
                {
                    appSettings["ConfigurationMessage"][kvp.Key] = kvp.Value;
                }
        
                var json = appSettings.ToString();
                await File.WriteAllTextAsync(appSettingsPath, json, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appsettings.json.");
            }
        }

        private async Task<JObject> GetAppSettings(CancellationToken stoppingToken)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var appSettingsJson = await File.ReadAllTextAsync(appSettingsPath, stoppingToken);
            return JObject.Parse(appSettingsJson);
        }

        private async Task<List<Guid>> ProcessLeadsWithBirthdays(CancellationToken stoppingToken)
        {
            var appSettings = await GetAppSettings(stoppingToken);
            var linkBaseUrl = appSettings["HttpClientSettings"]["BaseUrl"].ToString();
            var birthdayPeriod = appSettings["ConfigurationMessage"]["VipBirthdayPeriodDays"].ToString();

            var link = $"{linkBaseUrl}leads-birthdate?periodBdate={birthdayPeriod}";

            try
            {
                _logger.LogInformation("Getting leads from httpClient.");
                var leads = await _httpClient.Get<List<LeadDto>>(link, stoppingToken);
                return leads.Select(l => l.Id).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing leads by birthday.");
                return new List<Guid>();
            }
        }

        private async Task<List<Guid>> ProcessLeadsByTransactionCount(CancellationToken stoppingToken)
        {
            var appSettings = await GetAppSettings(stoppingToken);
            var linkBaseUrl = appSettings["HttpClientSettings"]["BaseUrl"].ToString();
            var transactionThreshold = appSettings["ConfigurationMessage"]["TransactionThreshold"].ToString();

            var link = $"{linkBaseUrl}api/transactions/by-period/{transactionThreshold}";

            try
            {
                _logger.LogInformation("Getting transactions from httpClient.");
                var transactions = await _httpClient.Get<List<TransactionResponse>>(link, stoppingToken);
                var res = _service.SetLeadStatusByTransactions(transactions);
                return res;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization error occurred while processing transactions.");
                return new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing leads.");
                return new List<Guid>();
            }
        }

        private async Task SendUpdateLeadStatusMessage(LeadsMessage newMessage)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                try
                {
                    _logger.LogInformation("Sending message to RabbitMQ with {count} leads.", newMessage.Leads.Count);
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
