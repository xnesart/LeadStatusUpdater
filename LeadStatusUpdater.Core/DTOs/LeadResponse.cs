using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.DTOs;

public class LeadResponse
{
    public Guid LeadId { get; set; }
    public Guid Id { get; set; }
    // public string Name { get; set; }
    // public string Mail { get; set; }
    // public string Phone { get; set; }
    // public string Address { get; set; }
    // public DateOnly BirthDate { get; set; }
    // public LeadStatus Status { get; set; }
    // public List<AccountDto> Accounts { get; set; }
    // public List<StatusHistoryDto> StatusHistory { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    
}