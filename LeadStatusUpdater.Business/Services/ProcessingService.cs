using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Responses;
using Messaging.Shared;

namespace LeadStatusUpdater.Business.Services;

public class ProcessingService : IProcessingService
{
    public List<LeadDto> SetLeadStatusByTransactions(List<TransactionResponse> responseList,
        List<LeadDto> leadsWithBirthday, int countOfTransactionsMustBiggestThen)
    {
        //rule 1
        var filledLeads = FillLeadsAtTransactions(responseList);
        var leads = ProcessLeads(filledLeads,42);
        

        return new List<LeadDto>();
    }

    public List<LeadDto> SetLeadsStatusByBirthday(List<LeadDto> leads, int countOfDays)
    {
        DateTime today = DateTime.Now.Date;
        DateTime thresholdDate = today.AddDays(-countOfDays);

        foreach (var lead in leads)
        {
            DateTime leadBirthdayThisYear = new DateTime(today.Year, lead.BirthDate.Month, lead.BirthDate.Day);

            if (leadBirthdayThisYear == today && lead.Status != LeadStatus.Administrator &&
                lead.Status != LeadStatus.Block)
            {
                lead.Status = LeadStatus.Vip;
            }
            else if (leadBirthdayThisYear >= thresholdDate && leadBirthdayThisYear < today &&
                     lead.Status != LeadStatus.Administrator && lead.Status != LeadStatus.Block)
            {
                lead.Status = LeadStatus.Regular;
            }
        }

        var res = new List<LeadDto>();
        res = leads;
        return res;
    }

    private List<LeadDto> FillLeadsAtTransactions(List<TransactionResponse> responseList)
    {
        if (responseList == null) return new List<LeadDto>();

        //создаем уникальных лидов на основе списка транзакций
        var list = new List<LeadDto>();
        var uniqueIds = new HashSet<Guid>();

        foreach (var transaction in responseList)
        {
            if (uniqueIds.Add(transaction.Id)) // Add returns false if the item already exists
            {
                var newLead = new LeadDto()
                {
                    Id = transaction.LeadId,
                };
                list.Add(newLead);
            }
        }

        foreach (var lead in list)
        {
            foreach (var transaction in responseList)
            {
                if (transaction.LeadId == lead.Id)
                {
                    var newTransaction = new TransactionDto()
                    {
                        Amount = transaction.Amount,
                        CurrencyType = transaction.CurrencyType,
                        TransactionType = transaction.TransactionType
                    };

                    if (lead.Accounts == null)
                    {
                        lead.Accounts = new List<AccountDto>();
                    }

                    if (lead.Accounts.Count == 0)
                    {
                        var newAccount = new AccountDto();
                        newAccount.Transactions = new List<TransactionDto>();
                        lead.Accounts.Add(newAccount);
                    }
                    
                    if (lead.Accounts[0].Transactions == null)
                    {
                        lead.Accounts[0].Transactions = new List<TransactionDto>();
                    }

                    lead.Accounts[0].Transactions.Add(newTransaction);
                }
            }
        }

        return list;
    }

    private List<LeadDto> ProcessLeads(List<LeadDto> leads, int transactionBiggerThen)
    {
        foreach (var lead in leads)
        {
            int transactionCount = 0;

            foreach (var account in lead.Accounts)
            {
                // Используем HashSet для учета уникальных трансферов между аккаунтами
                var transferSet = new HashSet<Guid>();

                foreach (var transaction in account.Transactions)
                {
                    // Учитываем только уникальные трансферы и не включаем Withdraw
                    if (transaction.TransactionType != TransactionType.Withdraw)
                    {
                        if (transaction.TransactionType == TransactionType.Transfer)
                        {
                            if (transferSet.Add(transaction.Id))
                            {
                                transactionCount++;
                            }
                        }
                        else
                        {
                            transactionCount++;
                        }
                    }
                }
            }
            
            if (transactionCount >= transactionBiggerThen)
            {
                lead.Status = LeadStatus.Vip; 
            }
        }
        return leads;
    }
}