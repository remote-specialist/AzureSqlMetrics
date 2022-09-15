using System.Text.Json.Serialization;

namespace AzureApi.Models
{
    public class GetSqlServersResponse
    {
        [JsonPropertyName("value")]
        public List<AzureSqlServer> Value { get; set; } = new List<AzureSqlServer>();
        
        public class AzureSqlServer
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            public string? ResourceGroupName => Id?.Split('/')[4];
            public string? ServerName => Id?.Split('/')[8];
        }
    }
}
