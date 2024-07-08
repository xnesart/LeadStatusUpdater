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
        _logger.LogInformation($"{_optionsMonitor.CurrentValue.TransactionThreshold} количество дней");
        var leads = ProcessLeads(responseList);
        Console.WriteLine();

        return leads;
    }

    public List<Guid> ProcessLeads(List<TransactionResponse> transactions)
    {
        var config = _optionsMonitor.CurrentValue;

        var now = DateTime.Now;
        var twoMonthsAgo = now.AddMonths(-2);
        var oneMonthAgo = now.AddMonths(-1);

        var leads = CreateListWithLeadsFromTransactions(transactions);

        foreach (var lead in leads)
        {
            if (lead.Status == LeadStatus.Administrator || lead.Status == LeadStatus.Block)
                continue;

            var isVip = false;

            var leadTransactions = transactions.Where(t => t.LeadId == lead.Id).ToList();

            var transactionCount = leadTransactions
                .Where(t => t.Date >= twoMonthsAgo && t.TransactionType != TransactionType.Withdraw)
                .GroupBy(t => new { t.Date, t.TransactionType })
                .Select(g => g.First())
                .Count();

            if (transactionCount >= config.TransactionThreshold) isVip = true;

            var totalDeposits = leadTransactions
                .Where(t => t.Date >= oneMonthAgo && t.TransactionType == TransactionType.Deposit)
                .Sum(t => t.AmountInRUB ?? 0);

            var totalWithdraws = leadTransactions
                .Where(t => t.Date >= oneMonthAgo && t.TransactionType == TransactionType.Withdraw)
                .Sum(t => t.AmountInRUB ?? 0);

            if (totalDeposits - totalWithdraws > config.DepositWithdrawDifferenceThreshold) isVip = true;

            lead.Status = isVip ? LeadStatus.Vip : LeadStatus.Regular;
        }

        var listWithGuids = leads.Where(t => t.Status == LeadStatus.Vip).Select(t => t.Id).ToList();

        return listWithGuids;
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