using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.Responses;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public TransactionType TransactionType { get; set; }
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public decimal? AmountInRUB { get; set; }
}