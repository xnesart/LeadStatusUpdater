using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Requests;
using System.Transactions;

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
    public async Task<decimal> ConvertToRublesAsync(int amount, CurrencyType currencyType)
    {
        // Здесь нужен код для получения актуального курса валют
        decimal exchangeRate = await GetExchangeRateAsync(currencyType);
        decimal amountInRubles = amount * exchangeRate;
        return amountInRubles;
    }
    // временная заглушка, переделать
    private async Task<decimal> GetExchangeRateAsync(CurrencyType currencyType)
    {
        switch (currencyType)
        {
            case CurrencyType.USD:
                return 90.00M;
            case CurrencyType.EUR:
                return 98.00M;
            case CurrencyType.JPY:
                return 0.58M;
            case CurrencyType.CNY:
                return 12.72M;
            case CurrencyType.RSD:
                return 0.84M;
            case CurrencyType.BGN:
                return 50.14M;
            case CurrencyType.ARS:
                return 0.10M;
            default:
                return 1M;
        }
    }
    public async Task CoutingDifferenceDepositAmountsAsync(GetLeadsRequest request)
    {
        foreach (var lead in request.Leads)
        {
            decimal totalDeposits = 0;
            decimal totalWithdraws = 0;

            foreach (var account in lead.Accounts)
            {
                foreach (var transaction in account.Transactions)
                {
                    if (transaction.Date >= DateTime.UtcNow.AddMonths(-1))
                    {
                        decimal amountInRubles = await ConvertToRublesAsync(transaction.Amount, transaction.CurrencyType);

                        switch (transaction.TransactionType)
                        {
                            case TransactionType.Deposit:
                                totalDeposits += amountInRubles;
                                break;
                            case TransactionType.Withdraw:
                                totalWithdraws += amountInRubles;
                                break;
                        }
                    }
                }
            }

            if (totalDeposits - totalWithdraws > 13000)
            {
                lead.Status = LeadStatus.Vip;
            }
        }
    }
    // надо подумать нужно ли сверять при запросе даты рождения с часовым поясом лида
    public async Task UpdateLeadStatusForBirthdayAsync(GetLeadsRequest request)
    {
        foreach (var lead in request.Leads)
        {
            if (lead.BirthDate.Month == DateTime.UtcNow.Month && lead.BirthDate.Day == DateTime.UtcNow.Day)
            {
                lead.Status = LeadStatus.Vip;
                await SaveTemporaryVipStatusAsync(lead.Id, TimeSpan.FromDays(14));
            }
        }
    }
    private async Task SaveTemporaryVipStatusAsync(Guid leadId, TimeSpan duration)
    {
        //подумать как передать в базу данных статус
        throw new NotImplementedException();
    }
}