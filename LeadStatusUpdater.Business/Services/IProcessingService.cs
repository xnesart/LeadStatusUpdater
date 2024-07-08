using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Business.Services;

public interface IProcessingService
{
    List<Guid> SetLeadStatusByTransactions(List<TransactionResponse> responseList);
}