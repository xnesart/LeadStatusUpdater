using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Business.Services;

public class ProcessingService : IProcessingService
{
    private const int TransactionThreshold = 42;
    private const decimal DepositWithdrawDifferenceThreshold = 13000;
    private const int VipBirthdayPeriodDays = 14;

    public List<Guid> SetLeadStatusByTransactions(List<TransactionResponse> responseList)
    {
        var leads = ProcessLeads(responseList);
        Console.WriteLine();

        return leads;
    }


    public List<Guid> SetLeadsStatusByBirthday(List<LeadDto> leads, int countOfDays)
    {
        var today = DateTime.Now.Date;
        var thresholdDate = today.AddDays(-countOfDays);

        var listOfVips = new List<Guid>();

        foreach (var lead in leads)
        {
            var leadBirthdayThisYear = new DateTime(today.Year, lead.BirthDate.Month, lead.BirthDate.Day);

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
        var now = DateTime.Now;
        var twoMonthsAgo = now.AddMonths(-2);
        var oneMonthAgo = now.AddMonths(-1);

        var leads = CreateListWithLeadsFromTransactions(transactions);

        foreach (var lead in leads)
        {
            if (lead.Status == LeadStatus.Administrator || lead.Status == LeadStatus.Block)
                // Не изменяем статус лида, если он админ или заблокирован
                continue;

            var isVip = false;

            var leadTransactions = transactions.Where(t => t.LeadId == lead.Id).ToList();

            // Check transactions in the last 2 months
            var transactionCount = leadTransactions
                .Where(t => t.Date >= twoMonthsAgo && t.TransactionType != TransactionType.Withdraw)
                .GroupBy(t => new { t.Date, t.TransactionType })
                .Select(g => g.First())
                .Count();

            if (transactionCount >= TransactionThreshold) isVip = true;

            // Check deposit and withdraw difference in the last month
            var totalDeposits = leadTransactions
                .Where(t => t.Date >= oneMonthAgo && t.TransactionType == TransactionType.Deposit)
                .Sum(t => t.AmountInRUB ?? 0);

            var totalWithdraws = leadTransactions
                .Where(t => t.Date >= oneMonthAgo && t.TransactionType == TransactionType.Withdraw)
                .Sum(t => t.AmountInRUB ?? 0);


            if (totalDeposits - totalWithdraws > DepositWithdrawDifferenceThreshold) isVip = true;

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
            if (result.All(lead => lead.Id != transaction.LeadId))
                result.Add(new LeadDto
                {
                    Id = transaction.LeadId
                });

        return result;
    }
}