using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Responses;
using LeadStatusUpdater.Core.Settings;
using Messaging.Shared;
using Microsoft.Extensions.Options;

namespace LeadStatusUpdater.Business.Services;

public class ProcessingService : IProcessingService
{
    private readonly int _transactionThreshold;
    private readonly decimal _depositWithdrawDifferenceThreshold;
    private readonly int _vipBirthdayPeriodDays;

    public ProcessingService(IOptions<LeadProcessingSettings> options)
    {
        var config = options.Value;
        _transactionThreshold = config.TransactionThreshold;
        _depositWithdrawDifferenceThreshold = config.DepositWithdrawDifferenceThreshold;
        _vipBirthdayPeriodDays = config.VipBirthdayPeriodDays;
    }

    public List<Guid> SetLeadStatusByTransactions(List<TransactionResponse> responseList)
    {
        var leads = ProcessLeads(responseList);
        Console.WriteLine();

        return leads;
    }

    public List<Guid> SetLeadsStatusByBirthday(List<LeadDto> leads, int countOfDays)
    {
        DateTime today = DateTime.Now.Date;
        DateTime thresholdDate = today.AddDays(-countOfDays);

        var listOfVips = new List<Guid>();

        foreach (var lead in leads)
        {
            DateTime leadBirthdayThisYear = new DateTime(today.Year, lead.BirthDate.Month, lead.BirthDate.Day);

            if (leadBirthdayThisYear == today && lead.Status != LeadStatus.Administrator &&
                lead.Status != LeadStatus.Block)
            {
                lead.Status = LeadStatus.Vip;
                listOfVips.Add(lead.Id);
            }
            else if (leadBirthdayThisYear < thresholdDate &&
                     lead.Status != LeadStatus.Administrator && lead.Status != LeadStatus.Block)
            {
                lead.Status = LeadStatus.Regular;
            }
        }

        return listOfVips;
    }

    public List<Guid> ProcessLeads(List<TransactionResponse> transactions)
    {
        //задаем значения в месяцах
        DateTime now = DateTime.Now;
        DateTime twoMonthsAgo = now.AddMonths(-2);
        DateTime oneMonthAgo = now.AddMonths(-1);

        var leads = CreateListWithLeadsFromTransactions(transactions);

        foreach (var lead in leads)
        {
            if (lead.Status == LeadStatus.Administrator || lead.Status == LeadStatus.Block)
            {
                // Не изменяем статус лида, если он админ или заблокирован
                continue;
            }

            bool isVip = false;

            var leadTransactions = transactions.Where(t => t.LeadId == lead.Id).ToList();

            // Check transactions in the last 2 months
            int transactionCount = leadTransactions
                .Where(t => t.Date >= twoMonthsAgo && t.TransactionType != TransactionType.Withdraw)
                .GroupBy(t => new { t.Date, t.TransactionType })
                .Select(g => g.First())
                .Count();

            if (transactionCount >= _transactionThreshold)
            {
                isVip = true;
            }

            // Check deposit and withdraw difference in the last month
            decimal totalDeposits = leadTransactions
                .Where(t => t.Date >= oneMonthAgo && t.TransactionType == TransactionType.Deposit)
                .Sum(t => t.AmountInRUB ?? 0);

            decimal totalWithdraws = leadTransactions
                .Where(t => t.Date >= oneMonthAgo && t.TransactionType == TransactionType.Withdraw)
                .Sum(t => t.AmountInRUB ?? 0);


            if ((totalDeposits - totalWithdraws) > _depositWithdrawDifferenceThreshold)
            {
                isVip = true;
            }

            Console.WriteLine(lead.Status);
            lead.Status = isVip ? LeadStatus.Vip : LeadStatus.Regular;
        }

        var listWithGuids = leads.Where(t => t.Status == LeadStatus.Vip).Select(t => t.Id).ToList();

        return listWithGuids;
    }

    private List<LeadDto> CreateListWithLeadsFromTransactions(List<TransactionResponse> transactions)
    {
        var result = new List<LeadDto>();

        foreach (var transaction in transactions)
        {
            if (result.All(lead => lead.Id != transaction.LeadId))
            {
                result.Add(new LeadDto()
                {
                    Id = transaction.LeadId
                });
            }
        }

        return result;
    }
}