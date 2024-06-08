using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Business.Services;

public interface IHttpClientService
{
    Task<GetLeadsResponse> Get(string urlForRequest,CancellationToken cancellationToken);
}