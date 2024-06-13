using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Enums;
using LeadStatusUpdater.Core.Requests;
using System.Transactions;
using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Business.Services;

public class ProcessingService : IProcessingService
{
    private const decimal DifferenceBetweenAmount = 13000;
    private const int MonthsAgo = -1;
    public void GetLeadStatus(GetLeadsResponse response)
    {
        var leadsResponse = new GetLeadsResponse()
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

        var res = CheckCountOfTransactions(leadsResponse, 1);
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
                    if (transaction.Date >= DateTime.UtcNow.AddMonths(MonthsAgo))
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

            if (totalDeposits - totalWithdraws > DifferenceBetweenAmount)
            {
                lead.Status = LeadStatus.Vip;
            }
        }
    }
    // надо подумать нужно ли сверять при запросе даты рождения с часовым поясом лида
    public void UpdateLeadStatusForBirthday(GetLeadsRequest request)
    {
        foreach (var lead in request.Leads)
        {
            if (lead.BirthDate.Month == DateTime.UtcNow.Month && lead.BirthDate.Day == DateTime.UtcNow.Day)
            {
                lead.Status = LeadStatus.Vip;
                SaveTemporaryVipStatusAsync(lead.Id, TimeSpan.FromDays(14));
            }
        }
    }
    private async Task SaveTemporaryVipStatusAsync(Guid leadId, TimeSpan duration)
    {
        //подумать как передать в базу данных статус
        throw new NotImplementedException();
    }
}