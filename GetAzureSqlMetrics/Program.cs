using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using Polly.Extensions.Http;
using Polly;
using AzureApi;
using Configurations;
using MetricStats;

namespace GetAzureSqlMetrics
{
    public class Program
    {
        private static readonly List<HttpStatusCode> TransientHttpStatusCodes =
            new()
            {
                HttpStatusCode.GatewayTimeout,
                HttpStatusCode.RequestTimeout,
                HttpStatusCode.ServiceUnavailable,
            };


        public static void Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder().ConfigureLogging((_, logging) =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            var configurationBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
            var configuration = configurationBuilder.Build();

            builder
                .ConfigureServices((_, services) =>
                services
                .AddSingleton<IAzureConfiguration>((s) =>
                {
                    return new AzureConfiguration(configuration);
                })
                .AddHttpClient<IAzureApiClient, AzureApiClient>()
                .AddPolicyHandler(GetRetryPolicy()));

            builder
                .ConfigureServices((_, services) =>
                services
                .AddSingleton<IStatsClient, StatsClient>());

            var host = builder.Build();
            var stats = host.Services.GetService<IStatsClient>();

            if(stats == null)
            {
                throw new NullReferenceException(nameof(stats));
            }

            stats.SaveReportAsync(
                configuration["AzureSqlServerName"], 
                int.Parse(configuration["DaysBack"]), 
                configuration["ReportPath"])
                .GetAwaiter().GetResult();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                   .HandleTransientHttpError()
                   .OrResult(msg => TransientHttpStatusCodes.Contains(msg.StatusCode))
                   .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}