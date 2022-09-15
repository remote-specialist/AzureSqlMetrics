using System;
using System.Collections.Generic;
using System.Linq;
namespace AzureApi.Models
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/rest/api/monitor/metric-definitions/list?tabs=HTTP for details
    /// </summary>
    public class GetMetricsRequest
    {
        public string? ResourceUri { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        /// <summary>
        /// Example: dtu_used, cpu_percent
        /// </summary>
        public List<string>? MetricNames { get; set; }
        /// <summary>
        /// Example: average, maximum
        /// </summary>
        public List<string>? Aggregations { get; set; }
        /// <summary>
        /// Example: PT1m, PT5m, PT15m
        /// </summary>
        public string? Interval { get; set; }
    }
}
