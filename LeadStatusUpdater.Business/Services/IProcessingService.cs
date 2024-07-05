using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Responses;
using Messaging.Shared;

namespace LeadStatusUpdater.Business.Services;

public interface IProcessingService
{
    List<Guid> SetLeadStatusByTransactions(List<TransactionResponse> responseList);
    List<Guid> SetLeadsStatusByBirthday(List<LeadDto> leads,int countOfDays);
}