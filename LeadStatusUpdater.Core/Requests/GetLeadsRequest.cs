using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Requests;

public class GetLeadsRequest
{
    public List<LeadDto> Leads { get; set; }
    public int TimePeriodInDays { get; set; }
}