using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Requests;

public class GetLeadsResponse
{
    public List<LeadDto> Leads { get; set; }
    public int TimePeriodInDays { get; set; }
}