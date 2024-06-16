using LeadStatusUpdater.Core.DTOs;

namespace LeadStatusUpdater.Core.Requests;

public class BirthdayVipEligibilityRequest
{
    public List<LeadDto> Leads { get; set; }
}