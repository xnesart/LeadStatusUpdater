using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Requests;

namespace LeadStatusUpdater.Business.Services;

public class ProcessingService : IProcessingService
{
    public void GetLeadStatus(GetLeadsResponse response)
    {
        var leadsRequest = new GetLeadsResponse()
        {
            Leads = new List<LeadDto>()
            {
                new LeadDto()
                {
                    Accounts = new List<AccountDto>()
                    {
                        new AccountDto()
                        {
                            Id = Guid.NewGuid(),
                            Currency = CurrencyType.RUB,
                            Status = AccountStatus.Active,
                            Transactions = new List<TransactionDto>()
                            {
                                new TransactionDto()
                                {
                                    Id = Guid.NewGuid(),
                                    Amount = 100,
                                    CurrencyType = CurrencyType.RUB,
                                    TransactionType = TransactionType.Deposit,
                                    Date = new DateTime(2024, 04, 20)
                                },
                                new TransactionDto()
                                {
                                    Id = Guid.NewGuid(),
                                    Amount = -100,
                                    CurrencyType = CurrencyType.RUB,
                                    TransactionType = TransactionType.Transfer,
                                    Date = new DateTime(2024, 04, 20)
                                }
                            }
                        },
                        new AccountDto()
                        {
                            Id = Guid.NewGuid(),
                            Currency = CurrencyType.USD,
                            Status = AccountStatus.Active,
                            Transactions = new List<TransactionDto>()
                            {
                                new TransactionDto()
                                {
                                    Id = Guid.NewGuid(),
                                    Amount = 1,
                                    CurrencyType = CurrencyType.USD,
                                    TransactionType = TransactionType.Transfer,
                                    Date = new DateTime(2024, 04, 20),
                                }
                            }
                        },
                    },
                    Address = "Ул. Петра Великого",
                    BirthDate = new DateTime(1990, 1, 20),
                    Name = "Петр",
                    Mail = "jerry@gmail.com",
                    Phone = "89774343545",
                    Status = LeadStatus.Regular
                }
            },
            TimePeriodInDays = 60
        };

        var res = CheckCountOfTransactions(leadsRequest, 1);
        Console.WriteLine(res);
    }

    private bool CheckCountOfTransactions(GetLeadsResponse response, int countOfTransactionsMustBiggestThen)
    {
        if (response.Leads == null) return false;

        int countOfDeposit = GetCountOfDepositTransactions(response);
        int countOfTransfer = GetCountOFTransferTransactions(response);

        int countOfAll = countOfDeposit + (countOfTransfer / 2);

        return countOfAll > countOfTransactionsMustBiggestThen;
    }

    private int GetCountOfTransactionsByType(GetLeadsResponse response, TransactionType type)
    {
        DateTime startDate = DateTime.Now.AddDays(-response.TimePeriodInDays);

        foreach (var lead in response.Leads)
        {
            if (lead != null && lead.Accounts != null)
            {
                foreach (var account in lead.Accounts)
                {
                    var transactions = account.Transactions.FindAll(t =>
                        t.TransactionType == type && t.Date >= startDate);

                    int transactionsCount = transactions.Count;

                    return transactionsCount;
                }
            }
        }

        return 0;
    }

    private int GetCountOfDepositTransactions(GetLeadsResponse response)
    {
        return GetCountOfTransactionsByType(response, TransactionType.Deposit);
    }

    private int GetCountOFTransferTransactions(GetLeadsResponse response)
    {
        return GetCountOfTransactionsByType(response, TransactionType.Transfer);
    }
}