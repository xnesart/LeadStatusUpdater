using System.Text.Json;
using LeadStatusUpdater.Business.Services;
using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Responses;
using MassTransit;
using Messaging.Shared;

namespace LeadStatusUpdater.Service;

public class Worker : BackgroundService
{
    private const int BirthdayPeriod = 20;
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProcessingService _service;
    private int _countOfStarts;
    private Timer _timer;

    public Worker(ILogger<Worker> logger, IProcessingService service, IHttpClientService httpClient,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _service = service;
        _httpClient = httpClient;
        _scopeFactory = scopeFactory;
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

        // Вычисляем время до следующего запуска в 02:00 ночи по московскому времени
        var now = DateTime.UtcNow;
        var moscowTime = now.AddHours(3);
        _logger.LogInformation("Current UTC time: {utcTime}, Moscow time: {moscowTime}", now, moscowTime);

        var scheduledTime = new DateTime(moscowTime.Year, moscowTime.Month, moscowTime.Day, 2, 0, 0);
        _logger.LogInformation("Scheduled time for first run: {scheduledTime}", scheduledTime);

        // Если текущее время после 02:00, то планируем на следующий день
        if (moscowTime.Hour >= 2)
        {
            scheduledTime = scheduledTime.AddDays(1);
            _logger.LogInformation("Current time is after 02:00, rescheduling to: {scheduledTime}", scheduledTime);
        }

        var dueTime = scheduledTime - moscowTime;
        _logger.LogInformation("Time until next scheduled run: {dueTime}", dueTime);

        // Запускаем таймер с интервалом в день
        _timer = new Timer(state => DoWork(cancellationToken), null, dueTime, TimeSpan.FromDays(1));

        return base.StartAsync(cancellationToken);
    }

    private void DoWork(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            var leadsWithBirthday = ProcessLeadsWithBirthdays(cancellationToken).GetAwaiter().GetResult();
            _logger.LogInformation("Processed leads with birthdays: {count}", leadsWithBirthday.Count);

            var leadsWithTransaction = ProcessLeadsByTransactionCount(cancellationToken, 15).GetAwaiter().GetResult();
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

    private async Task<List<Guid>> ProcessLeadsWithBirthdays(CancellationToken stoppingToken)
    {
        var link = $"https://194.87.210.5:12000/leads-birthdate?periodBdate={BirthdayPeriod}";
        _logger.LogInformation("Fetching leads with birthdays from: {link}", link);

        try
        {
            var leads = await _httpClient.Get<List<LeadDto>>(link, stoppingToken);
            _logger.LogInformation("Fetched {count} leads with birthdays.", leads.Count);

            var result = leads.Select(l => l.Id).ToList();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing leads by birthday.");
            return new List<Guid>();
        }
    }

    private async Task<List<Guid>> ProcessLeadsByTransactionCount(CancellationToken stoppingToken, int byDays)
    {
        var link = $"https://194.87.210.5:12000/api/transactions/by-period/{byDays}";
        _logger.LogInformation("Fetching transactions from: {link}", link);

        try
        {
            var transactions = await _httpClient.Get<List<TransactionResponse>>(link, stoppingToken);
            _logger.LogInformation("Fetched {count} transactions.", transactions.Count);

            _logger.LogInformation("Processing transactions to update lead statuses");
            var res = _service.SetLeadStatusByTransactions(transactions);

            _logger.LogInformation("Processed transactions into {count} lead statuses.", res.Count);
            return res;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error occurred while processing transactions");
            return new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing leads");
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
                _logger.LogInformation("Sending message to RabbitMQ with {count} leads", newMessage.Leads.Count);
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
