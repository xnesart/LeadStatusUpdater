using LeadStatusUpdater.Core.DTOs;

namespace Messaging.Shared;

public class LeadsGuidMessage
{
    public List<Guid> Leads { get; set; }
}