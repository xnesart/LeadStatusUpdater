using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Requests;

namespace LeadStatusUpdater.Business.Services;

public class ProcessingService : IProcessingService
{
    public void GetLeadStatus(GetLeadsRequest request)
    {
        var leadsRequest = new GetLeadsRequest()
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

    private bool CheckCountOfTransactions(GetLeadsRequest request, int countOfTransactionsMustBiggestThen)
    {
        var result = false;

        if (request.Leads != null)
        {
            int countOfDeposit = GetCountOfDepositTransactions(request);
            int countOfTrasfer = GetCountOFTransferTransactions(request);

            int countOfAll = 0;
            if (countOfTrasfer != 0)
            {
                countOfAll = countOfDeposit + countOfTrasfer / 2;
            }
            else
            {
                countOfAll = countOfDeposit;
            }

            if (countOfAll > countOfTransactionsMustBiggestThen)
            {
                result = true;
                return result;
            }

            return result;
        }

        return result;
    }

    private int GetCountOfDepositTransactions(GetLeadsRequest request)
    {
        DateTime startDate = DateTime.Now.AddDays(-request.TimePeriodInDays);

        foreach (var lead in request.Leads)
        {
            if (lead != null && lead.Accounts != null)
            {
                foreach (var account in lead.Accounts)
                {
                    if (lead.Accounts == null) continue;

                    var depositTransactions = account.Transactions.FindAll(t =>
                        t.TransactionType == TransactionType.Deposit && t.Date >= startDate);

                    int depositCount = depositTransactions.Count;

                    return depositCount;
                }
            }
        }

        return 0;
    }

    private int GetCountOFTransferTransactions(GetLeadsRequest request)
    {
        DateTime startDate = DateTime.Now.AddDays(-request.TimePeriodInDays);

        foreach (var lead in request.Leads)
        {
            if (lead != null && lead.Accounts != null)
            {
                foreach (var account in lead.Accounts)
                {
                    if (lead.Accounts == null) continue;

                    var transferTransactions = account.Transactions.FindAll(t =>
                        t.TransactionType == TransactionType.Transfer && t.Date >= startDate);
                    int transferCount = transferTransactions.Count;
                    return transferCount;
                }
            }
        }

        return 0;
    }
}