using LeadStatusUpdater.Core.Enums;

namespace LeadStatusUpdater.Core.DTOs;

public class StatusHistoryDto
{
    public Guid Id { get; set; }
    public LeadDto Lead { get; set; }
    public LeadStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
}