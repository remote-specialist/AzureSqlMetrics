using AzureApi.Models;
using Configurations;
using Configurations.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AzureApi
{
    public class AzureApiClient : IAzureApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AzureConfigurationModel _azure;
        private string _accessToken;

        public AzureApiClient(HttpClient httpClient, IAzureConfiguration azureConfiguration)
        {
            _httpClient = httpClient;
            _azure = azureConfiguration.GetConfiguration();
            _accessToken = string.Empty;
        }

        public async Task<GetSqlServersResponse> GetSqlServersAsync()
        {
            var requestUri = $"https://management.azure.com/subscriptions/{_azure.SubscriptionId}/providers/Microsoft.Sql/servers?api-version=2021-02-01-preview";
            var responseMessage = await HttpGetAsync(requestUri);
            var text = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<GetSqlServersResponse>(text);
            if (response == null)
            {
                throw new NullReferenceException($"Cannot deserialize {nameof(GetSqlServersResponse)}");
            }

            return response;
        }

        public async Task<GetSqlDatabasesResponse> GetSqlDatabasesAsync(GetSqlServersResponse.AzureSqlServer server)
        {
            if(string.IsNullOrEmpty(server.ResourceGroupName) || string.IsNullOrEmpty(server.ServerName))
            {
                throw new ArgumentException("ResourceGroupName and ServerName cannot be null or epmty", nameof(server));
            }

            var requestUri =
                $"https://management.azure.com/subscriptions/{_azure.SubscriptionId}/resourceGroups/{server.ResourceGroupName}/providers/Microsoft.Sql/servers/{server.ServerName}/databases?api-version=2021-02-01-preview";
            var responseMessage = await HttpGetAsync(requestUri);

            var text = await responseMessage.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<GetSqlDatabasesResponse>(text);

            if (response == null)
            {
                throw new NullReferenceException($"Cannot deserialize {nameof(GetSqlDatabasesResponse)}");
            }

            return response;
        }

        public async Task<GetMetricsResponse> GetMetricsAsync(GetMetricsRequest request)
        {
            if(request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
                
            if(string.IsNullOrEmpty(request.ResourceUri))
            {
                throw new ArgumentNullException("ResourceUri cannot be null or empty", nameof(request.ResourceUri));
            }

            var interval = string.IsNullOrEmpty(request.Interval) ? string.Empty : request.Interval;

            var aggregation = (request.Aggregations == null || request.Aggregations.Count == 0 || request.Aggregations.All(x => string.IsNullOrEmpty(x))) 
                ? string.Empty
                : string.Join(',', request.Aggregations.Distinct().ToList());

            var metricNames = (request.MetricNames == null || request.MetricNames.Count == 0 || request.MetricNames.All(x => string.IsNullOrEmpty(x))) 
                ? string.Empty
                : string.Join(',', request.MetricNames.Distinct().ToList());

            var end = DateTime.Now;
            var start = end.AddDays(-1);
            if (request.End == null && request.Start != null)
            {
                start = (DateTime) request.Start;
                end = start.AddDays(1);
            }
            if (request.End != null && request.Start == null)
            {
                end = (DateTime) request.End;
                start = end.AddDays(-1);
            }
            if (request.End != null && request.Start != null)
            {
                if(request.End <= request.Start)
                {
                    throw new ArgumentException($"{nameof(GetMetricsAsync)} End must be greater than Start! Start: {request.Start}, End: {request.End}");
                }
                else
                {
                    start = (DateTime) request.Start;
                    end = (DateTime) request.End;
                }
            }

            var requestUri = $"https://management.azure.com/{request.ResourceUri.Trim('/')}" +
                "/providers/Microsoft.Insights/metrics?api-version=2018-01-01" +
                $"&timespan={Uri.EscapeDataString($"{start.ToUniversalTime():o}/{end.ToUniversalTime():o}")}";

            if(!string.IsNullOrEmpty(metricNames))
            {
                requestUri += $"&metricnames={metricNames}";
            }
            if(!string.IsNullOrEmpty(aggregation))
            {
                requestUri += $"&aggregation={aggregation}";
            }
            if(!string.IsNullOrEmpty(interval))
            {
                requestUri += $"&interval={interval}";
            }

            var responseMessage = await HttpGetAsync(requestUri);
            var responseContent = await responseMessage.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<GetMetricsResponse>(responseContent);
            if (result == null)
            {
                throw new InvalidOperationException(nameof(result));
            }

            return result;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if(!IsTokenExpired())
            {
                return _accessToken;
            }

            if(_azure == null 
                || string.IsNullOrEmpty(_azure.TenantId) 
                || string.IsNullOrEmpty(_azure.ClientId) 
                || string.IsNullOrEmpty(_azure.ClientSecret))
            {
                throw new ArgumentException(nameof(_azure));
            }

            var resourceUrl = "https://management.azure.com/";
            var requestUrl = $"https://login.microsoftonline.com/{_azure.TenantId}/oauth2/token";

            var dict = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _azure.ClientId },
                { "client_secret", _azure.ClientSecret },
                { "resource", resourceUrl }
            };

            var requestBody = new FormUrlEncodedContent(dict);
            var response = await _httpClient.PostAsync(requestUrl, requestBody);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var azureAccessToken = JsonSerializer.Deserialize<AzureAccessTokenModel>(responseContent);
            if(azureAccessToken == null || string.IsNullOrEmpty(azureAccessToken.Token))
            {
                throw new InvalidOperationException(nameof(azureAccessToken));
            }

            _accessToken = azureAccessToken.Token;
            return _accessToken;
        }

        private bool IsTokenExpired()
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                return true;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = (JwtSecurityToken) handler.ReadToken(_accessToken);
            return DateTime.UtcNow.AddMinutes(-5) >= token.ValidTo;
        }

        private async Task<HttpResponseMessage> HttpGetAsync(string requestUri)
        {
            await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
            var responseMessage = await _httpClient.GetAsync(requestUri);
            responseMessage.EnsureSuccessStatusCode();

            return responseMessage;
        }
    }
}