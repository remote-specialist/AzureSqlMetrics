using System.Text.Json.Serialization;

namespace AzureApi.Models
{
    public class GetSqlDatabasesResponse
    {
        [JsonPropertyName("value")]
        public List<AzureSqlDatabase> Value { get; set; } = new List<AzureSqlDatabase>();

        public class AzureSqlDatabase
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("name")]
            public string? Name { get; set; }
            [JsonPropertyName("sku")]
            public SkuModel? Sku { get; set; }

            [JsonPropertyName("kind")]
            public string Kind { get; set; } = string.Empty;

            public class SkuModel
            {
                [JsonPropertyName("name")]
                public string? Name { get; set; }
                [JsonPropertyName("capacity")]
                public int Capacity { get; set; }
            }
        }
    }
}
