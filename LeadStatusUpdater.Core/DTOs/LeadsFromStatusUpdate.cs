using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.DTOs;

public class LeadsFromStatusUpdate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateOnly BirthDate { get; set; }
    public LeadStatus Status { get; set; }
    public List<AccountDto> Accounts { get; set; }
    public List<StatusHistoryDto> StatusHistory { get; set; }
}