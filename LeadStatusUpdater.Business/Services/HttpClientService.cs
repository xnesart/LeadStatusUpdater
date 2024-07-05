using LeadStatusUpdater.Core.Settings;
using Microsoft.Extensions.Options;
using RestSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeadStatusUpdater.Business.Services
{
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClientSettings _settings;

        public HttpClientService(IOptions<HttpClientSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<T> Get<T>(string urlForRequest, CancellationToken cancellationToken)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };

            var options = new RestClientOptions(_settings.BaseUrl)
            {
                ConfigureMessageHandler = _ => handler,
                Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
            };

            var client = new RestClient(options);
            var request = new RestRequest(urlForRequest);

            var response = await client.ExecuteAsync(request, Method.Get, cancellationToken);

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode} and message: {response.ErrorMessage}, Content: {response.Content}");
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(response.Content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (result == null)
                {
                    throw new HttpRequestException("Deserialized response is null");
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new HttpRequestException("Failed to deserialize response", ex);
            }
        }
    }
}
