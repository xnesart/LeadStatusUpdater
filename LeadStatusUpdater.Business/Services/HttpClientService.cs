using LeadStatusUpdater.Core.Settings;
using Microsoft.Extensions.Options;
using RestSharp;

namespace LeadStatusUpdater.Business.Services;

public class HttpClientService : IHttpClientService
{
    private readonly HttpClientSettings _settings;
    public HttpClientService(IOptions<HttpClientSettings> settings)
    {
        _settings = settings.Value;
    }
    
    public async Task<T> Get<T>(string urlForRequest,CancellationToken cancellationToken)
    {
        var options = new RestClientOptions(_settings.BaseUrl);
    
        var client = new RestClient(options);

        var request = new RestRequest(urlForRequest);
        
        var response = await client.GetAsync<T>(request, cancellationToken);
        
        return response;
    }
}