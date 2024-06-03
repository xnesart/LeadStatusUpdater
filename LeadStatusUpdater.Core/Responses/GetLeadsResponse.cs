using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Responses;

public class GetLeadsResponse
{
    public List<LeadDto> Leads { get; set; }
    public int TimePeriodInDays { get; set; }
}