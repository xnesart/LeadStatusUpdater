using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Responses;
using LeadStatusUpdater.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeadStatusUpdater.Business.Services;

public class ProcessingService : IProcessingService
{
    private readonly ILogger<ProcessingService> _logger;
    private readonly IOptionsMonitor<LeadProcessingSettings> _optionsMonitor;

    public ProcessingService(IOptionsMonitor<LeadProcessingSettings> optionsMonitor, ILogger<ProcessingService> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public List<Guid> SetLeadStatusByTransactions(List<TransactionResponse> responseList)
    {
        if (responseList == null)
        {
            _logger.LogWarning("Received null transaction response list.");
            return new List<Guid>();
        }

        LogConfigurationSettings();
        return ProcessLeads(responseList);
    }

    private void LogConfigurationSettings()
    {
        var settings = _optionsMonitor.CurrentValue;
        _logger.LogInformation($"BillingPeriodForTransactionsCount: {settings.BillingPeriodForTransactionsCount}");
        _logger.LogInformation($"TransactionsCount: {settings.TransactionsCount}");
        _logger.LogInformation($"BillingPeriodForDifferenceBetweenDepositAndWithdraw: {settings.BillingPeriodForDifferenceBetweenDepositAndWithdraw}");
        _logger.LogInformation($"DifferenceBetweenDepositAndWithdraw: {settings.DifferenceBetweenDepositAndWithdraw}");
        _logger.LogInformation($"BillingPeriodForBirthdays: {settings.BillingPeriodForBirthdays}");
    }

    private List<Guid> ProcessLeads(List<TransactionResponse> transactions)
    {
        var leads = CreateUniqueLeadsList(transactions);
        var leadsWithRightCountOfTransactions = GetLeadsWithRightNumberTransactions(leads, transactions);
        var leadsWithDepositMoreThanWithdraw = GetLeadsWithDepositMoreThanWithdraw(leads, transactions);

        return leadsWithRightCountOfTransactions
            .Union(leadsWithDepositMoreThanWithdraw)
            .Select(lead => lead.Id)
            .Distinct()
            .ToList();
    }

    private List<LeadDto> GetLeadsWithRightNumberTransactions(List<LeadDto> leads, List<TransactionResponse> transactionsList)
    {
        var config = _optionsMonitor.CurrentValue;
        var startDate = DateTime.Now.AddDays(-config.BillingPeriodForTransactionsCount);
        return leads.Where(lead =>
            transactionsList.Count(t => t.LeadId == lead.Id && t.Date >= startDate && t.TransactionType != TransactionType.Withdraw) > config.TransactionsCount
        ).ToList();
    }

    private List<LeadDto> GetLeadsWithDepositMoreThanWithdraw(List<LeadDto> leads, List<TransactionResponse> transactionsList)
    {
        var config = _optionsMonitor.CurrentValue;
        var startDate = DateTime.Now.AddDays(-config.BillingPeriodForDifferenceBetweenDepositAndWithdraw);
        return leads.Where(lead =>
        {
            var totalDeposits = transactionsList.Where(t => t.LeadId == lead.Id && t.Date >= startDate && t.TransactionType == TransactionType.Deposit).Sum(t => t.AmountInRUB ?? 0);
            var totalWithdraws = transactionsList.Where(t => t.LeadId == lead.Id && t.Date >= startDate && t.TransactionType == TransactionType.Withdraw).Sum(t => t.AmountInRUB ?? 0);
            return totalDeposits - totalWithdraws > config.DifferenceBetweenDepositAndWithdraw;
        }).ToList();
    }

    private List<LeadDto> CreateUniqueLeadsList(List<TransactionResponse> transactions)
    {
        return transactions.GroupBy(t => t.LeadId).Select(g => new LeadDto { Id = g.Key }).ToList();
    }
}