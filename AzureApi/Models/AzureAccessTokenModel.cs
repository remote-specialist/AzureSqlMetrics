using System.Text.Json.Serialization;


namespace AzureApi.Models
{
    public class AzureAccessTokenModel
    {
        [JsonPropertyName("access_token")]
        public string? Token { get; set; }
    }
}
