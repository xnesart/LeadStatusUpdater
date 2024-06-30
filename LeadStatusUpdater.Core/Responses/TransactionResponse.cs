using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.Responses;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public CurrencyType CurrencyType { get; set; }
    public Guid LeadId { get; set; }
}