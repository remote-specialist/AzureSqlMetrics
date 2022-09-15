using AzureApi.Models;

namespace AzureApi
{
    public interface IAzureApiClient
    {
        Task<string> GetAccessTokenAsync();

        Task<GetMetricsResponse> GetMetricsAsync(GetMetricsRequest request);
        Task<GetSqlServersResponse> GetSqlServersAsync();
        Task<GetSqlDatabasesResponse> GetSqlDatabasesAsync(GetSqlServersResponse.AzureSqlServer server);
    }
}
