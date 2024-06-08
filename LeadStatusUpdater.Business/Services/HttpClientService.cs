using LeadStatusUpdater.Core.Responses;
using RestSharp;

namespace LeadStatusUpdater.Business.Services;

public class HttpClientService : IHttpClientService
{
    public HttpClientService()
    {
    }
    private const string GetLeadsUrl = "https://194.87.210.5:11000/api/";
    
    public async Task<GetLeadsResponse> GetLeads(CancellationToken cancellationToken)
    {
        var options = new RestClientOptions(GetLeadsUrl);
    
        var client = new RestClient(options);

        var request = new RestRequest("report/leads-with-transactions");
        
        var response = await client.GetAsync<GetLeadsResponse>(request, cancellationToken);
        
        return response;
    }
}