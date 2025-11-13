using InvoiceRobot.Admin.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InvoiceRobot.Admin.Services;

/// <summary>
/// Service for querying Application Insights logs
/// </summary>
public class LogsService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private string? _connectionString;
    private string? _appId;
    private string? _apiKey;

    public LogsService(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public void SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;

        // Parse Application Insights connection string
        // Format: InstrumentationKey=xxx;IngestionEndpoint=https://...;LiveEndpoint=https://...
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("InstrumentationKey="))
            {
                _appId = part.Substring("InstrumentationKey=".Length);
            }
        }
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    /// <summary>
    /// Query logs from Application Insights
    /// </summary>
    public async Task<List<LogEntry>> QueryLogsAsync(
        TimeSpan timeRange,
        string? severityFilter = null,
        string? operationNameFilter = null)
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Connection string not set");

        var logs = new List<LogEntry>();

        try
        {
            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Application Insights App ID tai API Key puuttuu");
                return logs;
            }

            _logger.LogInformation($"Querying logs: timeRange={timeRange}, severity={severityFilter}, operation={operationNameFilter}");

            // Build KQL query
            var timeAgo = timeRange.TotalMinutes;
            var kqlQuery = $@"
                union traces, exceptions
                | where timestamp > ago({timeAgo}m)";

            if (!string.IsNullOrEmpty(severityFilter))
            {
                kqlQuery += $" | where severityLevel == '{severityFilter}'";
            }

            if (!string.IsNullOrEmpty(operationNameFilter))
            {
                kqlQuery += $" | where operation_Name contains '{operationNameFilter}'";
            }

            kqlQuery += " | order by timestamp desc | limit 1000";

            // Query Application Insights using REST API
            var queryUrl = $"https://api.applicationinsights.io/v1/apps/{_appId}/query";

            var request = new HttpRequestMessage(HttpMethod.Post, queryUrl);
            request.Headers.Add("x-api-key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { query = kqlQuery }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Application Insights query failed: {response.StatusCode}");
                return logs;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var queryResult = JsonSerializer.Deserialize<AppInsightsQueryResult>(responseContent);

            if (queryResult?.tables != null && queryResult.tables.Length > 0)
            {
                var table = queryResult.tables[0];
                if (table.columns == null || table.rows == null)
                    return logs;

                var timestampIdx = Array.IndexOf(table.columns.Select(c => c.name).ToArray(), "timestamp");
                var severityIdx = Array.IndexOf(table.columns.Select(c => c.name).ToArray(), "severityLevel");
                var operationIdx = Array.IndexOf(table.columns.Select(c => c.name).ToArray(), "operation_Name");
                var messageIdx = Array.IndexOf(table.columns.Select(c => c.name).ToArray(), "message");

                foreach (var row in table.rows)
                {
                    if (row != null && row.Length > 0)
                    {
                        logs.Add(new LogEntry
                        {
                            Timestamp = DateTime.Parse(row[timestampIdx]?.ToString() ?? DateTime.UtcNow.ToString()),
                            SeverityLevel = row[severityIdx]?.ToString() ?? "Information",
                            OperationName = row[operationIdx]?.ToString() ?? string.Empty,
                            Message = row[messageIdx]?.ToString() ?? string.Empty
                        });
                    }
                }
            }

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe lokien haussa");
            return logs;
        }
    }

    /// <summary>
    /// Get recent errors from Application Insights
    /// </summary>
    public async Task<List<LogEntry>> GetRecentErrorsAsync(int count = 50)
    {
        return await QueryLogsAsync(
            TimeSpan.FromHours(24),
            severityFilter: "Error");
    }

    // Helper classes for deserializing Application Insights query response
    private class AppInsightsQueryResult
    {
        public AppInsightsTable[]? tables { get; set; }
    }

    private class AppInsightsTable
    {
        public string? name { get; set; }
        public AppInsightsColumn[]? columns { get; set; }
        public object[][]? rows { get; set; }
    }

    private class AppInsightsColumn
    {
        public string? name { get; set; }
        public string? type { get; set; }
    }
}
