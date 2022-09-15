using AzureApi;
using AzureApi.Models;
using MetricStats.Models;
using System.Text.Json;

namespace MetricStats
{
    public class StatsClient : IStatsClient
    {
        private readonly IAzureApiClient _api;
        private static readonly int _tasksLimit = 10;
        public StatsClient(IAzureApiClient api)
        {
            _api = api;
        }

        public async Task SaveReportAsync(string serverName, int daysBack, string reportPath)
        {
            if(string.IsNullOrEmpty(serverName))
            {
                throw new ArgumentNullException(nameof(serverName));
            }

            if(string.IsNullOrEmpty(reportPath))
            {
                throw new ArgumentNullException(nameof(reportPath));
            }

            if (daysBack <= 0 || daysBack > 30)
            {
                throw new ArgumentNullException(nameof(daysBack));
            }

            var end = DateTime.Now;
            var getServersResponse = await _api.GetSqlServersAsync();
            var server = getServersResponse.Value.Single(s => string.Equals(serverName, s.ServerName, StringComparison.OrdinalIgnoreCase));

            var getDatabasesResponse = await _api.GetSqlDatabasesAsync(server);
            var databases = getDatabasesResponse.Value
                .Where(d => 
                !string.Equals("master", d.Name, StringComparison.OrdinalIgnoreCase)
                && !d.Kind.Contains("vcore")); // skip vCore Purchase Model databases

            var records = new List<VisualRecordModel>();
            foreach(var database in databases)
            {
                var recordsPerDatabse = await GetSqlMetricsAsync(database, end, daysBack);
                records.AddRange(recordsPerDatabse);
            }

            File.WriteAllText(Path.Combine(reportPath), JsonSerializer.Serialize(records));
        }

        private async Task<List<VisualRecordModel>> GetSqlMetricsAsync(GetSqlDatabasesResponse.AzureSqlDatabase database, DateTime end, int daysBack)
        {
            var frames = GetPagingIntervalsByDays(end, daysBack);
            var queues = GetQueues(frames, _tasksLimit);

            var models = new List<VisualRecordModel>();
            foreach (var queue in queues)
            {
                var tasks = new List<Task<GetMetricsResponse>>();
                foreach (var timeFrame in queue)
                {
                    tasks.Add(_api.GetMetricsAsync(new GetMetricsRequest
                    {
                        ResourceUri = database.Id,
                        Start = timeFrame.Start,
                        End = timeFrame.End,
                        Aggregations = new List<string> { "average", "maximum" },
                        MetricNames = new List<string> { "dtu_used" },
                        Interval = "PT1m"
                    }));
                }

                var responses = await Task.WhenAll(tasks);
                foreach(var response in responses)
                {
                    if(response != null && response.Value.Count > 0 && response.Value[0].TimeSeries.Count > 0 
                        && response.Value[0].TimeSeries[0].Data.Count > 0)
                    {
                        foreach (var item in response.Value[0].TimeSeries[0].Data)
                        {
                            models.Add(new VisualRecordModel
                            {
                                TimeStamp = item.TimeStamp,
                                Average = item.Average,
                                Maximum = item.Maximum,
                                Name = database.Name
                            });
                        }
                    }
                }
            }

            return models;
        }

        private static List<TimeFrameModel> GetPagingIntervalsByDays(DateTime end, int daysBack)
        {
            var frames = new List<TimeFrameModel>();

            var start = end.AddDays(-daysBack);
            var interval = TimeSpan.FromDays(1);

            while (start < end)
            {
                var frame = new TimeFrameModel
                {
                    Start = start,
                    End = start + interval
                };

                start += interval;
                frames.Add(frame);
            }

            return frames;
        }

        private static List<List<T>> GetQueues<T>(List<T> items, int limit)
        {
            var queues = new List<List<T>>();
            for (var i = 0; i < items.Count; i++)
            {
                if (i % limit == 0)
                {
                    var queue = new List<T>();
                    queues.Add(queue);
                }

                queues.Last().Add(items[i]);
            }

            return queues;
        }
    }
}