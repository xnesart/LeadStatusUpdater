using LeadStatusUpdater.Core.Requests;

namespace LeadStatusUpdater.Business.Services;

public interface IProcessingService
{
    void GetLeadStatus(GetLeadsRequest request);
}