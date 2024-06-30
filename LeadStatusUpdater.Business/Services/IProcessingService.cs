using LeadStatusUpdater.Core.DTOs;
using LeadStatusUpdater.Core.Responses;
using Messaging.Shared;

namespace LeadStatusUpdater.Business.Services;

public interface IProcessingService
{
    List<LeadDto> SetLeadStatusByTransactions(List<TransactionResponse> responseList,List<LeadDto> leadsWithBirthday, int countOfTransactionsMustBiggestThen);
    List<LeadDto> SetLeadsStatusByBirthday(List<LeadDto> leads,int countOfDays);
}