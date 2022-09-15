namespace MetricStats
{
    public interface IStatsClient
    {
        Task SaveReportAsync(string serverName, int daysBack, string reportPath);
    }
}
