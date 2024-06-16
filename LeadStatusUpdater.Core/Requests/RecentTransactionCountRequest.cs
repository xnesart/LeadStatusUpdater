using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Requests;

public class RecentTransactionCountRequest
{
    public List<LeadDto> Leads { get; set; }
    public int TimePeriodInDays { get; set; } = 60;
    public int MinimumTransactionCount { get; set; } = 42;
}