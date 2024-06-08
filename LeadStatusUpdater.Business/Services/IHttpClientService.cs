using LeadStatusUpdater.Core.Responses;

namespace LeadStatusUpdater.Business.Services;

public interface IHttpClientService
{
    Task<T> Get<T>(string urlForRequest, CancellationToken cancellationToken);
}