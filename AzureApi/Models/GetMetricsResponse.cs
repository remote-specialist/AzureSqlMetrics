using System.Text.Json.Serialization;

namespace AzureApi.Models
{
    public class GetMetricsResponse
    {
        [JsonPropertyName("value")]
        public List<MetricItem> Value { get; set; } = new List<MetricItem>();

        public class MetricItem
        {
            [JsonPropertyName("name")]
            public MetricName Name { get; set; } = new MetricName();

            [JsonPropertyName("timeseries")]
            public List<TimeSeriesItem> TimeSeries { get; set; } = new List<TimeSeriesItem>();
            public class MetricName
            {
                [JsonPropertyName("value")]
                public string Value { get; set; } = string.Empty;
            }

            public class TimeSeriesItem
            {
                [JsonPropertyName("data")]
                public List<DataItem> Data { get; set; } = new List<DataItem>();
                public class DataItem
                {
                    [JsonPropertyName("timeStamp")]
                    public string? TimeStamp { get; set; }
                    [JsonPropertyName("average")]
                    public double? Average { get; set; }
                    [JsonPropertyName("maximum")]
                    public double? Maximum { get; set; }
                }
            }
        }
    }
}
