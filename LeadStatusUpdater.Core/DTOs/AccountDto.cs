using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.DTOs;

public class AccountDto
{
    public Guid Id { get; set; }
    public CurrencyType Currency { get; set; }
    public AccountStatus Status { get; set; }
    public LeadDto Lead { get; set; }
    public List<TransactionDto> Transactions { get; set; }
}