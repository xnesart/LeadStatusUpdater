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
        _logger.LogInformation(
            $"количество дней, за которые лид должен был совершить нужное количество транзакций {_optionsMonitor.CurrentValue.BillingPeriodForTransactionsCount} ");
        _logger.LogInformation(
            $"количество транзакций {_optionsMonitor.CurrentValue.TransactionsCount} ");
        _logger.LogInformation(
            $"количество дней, за которые считаем разницу между депозитом и виздроу {_optionsMonitor.CurrentValue.BillingPeriodForDifferenceBetweenDepositAndWithdraw} ");
        _logger.LogInformation(
            $"разница между суммой депозитов и суммой withdraw лида должна быть больше {_optionsMonitor.CurrentValue.DifferenceBetweenDepositAndWithdraw} ");
        _logger.LogInformation(
            $"запрос лидов с днями рождения за {_optionsMonitor.CurrentValue.BillingPeriodForBirthdays} дней");

        var leads = ProcessLeads(responseList);

        return leads;
    }

    private List<Guid> ProcessLeads(List<TransactionResponse> transactions)
    {
        var config = _optionsMonitor.CurrentValue;

        var leads = CreateListWithLeadsFromTransactions(transactions);

        var leadsWithRightCountOfTransactions = GetLeadsWithRightNumberTransactions(leads, transactions);
        var leadsWithDepositMoreThanWithdraw = GetLeadsWithDepositMoreThanWithdraw(leads, transactions);
        
        var generalLeadsGuids = leadsWithRightCountOfTransactions
            .Union(leadsWithDepositMoreThanWithdraw)
            .Select(lead => lead.Id)
            .Distinct()
            .ToList();

        return generalLeadsGuids;
    }

    private List<LeadDto> GetLeadsWithRightNumberTransactions(List<LeadDto> leads,
        List<TransactionResponse> transactionsList)
    {
        var countOfDays = _optionsMonitor.CurrentValue.BillingPeriodForTransactionsCount;
        var countOfTransactions = _optionsMonitor.CurrentValue.TransactionsCount;

        var startDate = DateTime.Now.AddDays(-countOfDays);
        var result = new List<LeadDto>();

        foreach (var lead in leads)
        {
            var leadTransactions = transactionsList.Where(t => t.LeadId == lead.Id).ToList();
            var transactionsAmount = leadTransactions
                .Where(t => t.Date >= startDate && t.TransactionType != TransactionType.Withdraw)
                .GroupBy(t => new { t.Date, t.TransactionType })
                .Select(g => g.First())
                .Count();

            if (transactionsAmount > countOfTransactions) result.Add(lead);
        }

        return result;
    }

    private List<LeadDto> GetLeadsWithDepositMoreThanWithdraw(List<LeadDto> leads,
        List<TransactionResponse> transactionsList)
    {
        var countOfDays = _optionsMonitor.CurrentValue.BillingPeriodForDifferenceBetweenDepositAndWithdraw;
        var difference = _optionsMonitor.CurrentValue.DifferenceBetweenDepositAndWithdraw;

        var startDate = DateTime.Now.AddDays(-countOfDays);
        var result = new List<LeadDto>();

        foreach (var lead in leads)
        {
            var totalDeposits = transactionsList
                .Where(t => t.Date >= startDate && t.TransactionType == TransactionType.Deposit)
                .Sum(t => t.AmountInRUB ?? 0);

            var totalWithdraws = transactionsList
                .Where(t => t.Date >= startDate && t.TransactionType == TransactionType.Withdraw)
                .Sum(t => t.AmountInRUB ?? 0);

            if (totalDeposits - totalWithdraws > difference) result.Add(lead);
        }

        return result;
    }

    private List<LeadDto> CreateListWithLeadsFromTransactions(List<TransactionResponse> transactions)
    {
        var result = new List<LeadDto>();

        foreach (var transaction in transactions)
            if (result.All(lead => lead.Id != transaction.LeadId))
                result.Add(new LeadDto
                {
                    Id = transaction.LeadId
                });

        return result;
    }
}