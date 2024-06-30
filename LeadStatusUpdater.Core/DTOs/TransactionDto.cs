using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public AccountDto Account { get; set; }
    public TransactionType TransactionType { get; set; }
    public CurrencyType CurrencyType { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}