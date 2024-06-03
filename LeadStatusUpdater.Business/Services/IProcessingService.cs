using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Business.Services;

public interface IProcessingService
{
    void GetLeadStatus(GetLeadsResponse response);
}