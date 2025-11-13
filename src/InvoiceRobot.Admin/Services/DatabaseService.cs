using InvoiceRobot.Admin.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace InvoiceRobot.Admin.Services;

/// <summary>
/// Service for querying the InvoiceRobot SQL database
/// </summary>
public class DatabaseService
{
    private readonly ILogger _logger;
    private string? _connectionString;

    public DatabaseService(ILogger logger)
    {
        _logger = logger;
    }

    public void SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Get paginated list of invoices
    /// </summary>
    public async Task<List<InvoiceDto>> GetInvoicesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? statusFilter = null,
        string? searchTerm = null)
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Connection string not set");

        var invoices = new List<InvoiceDto>();

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT
                    i.Id,
                    i.NetvisorInvoiceKey,
                    i.InvoiceNumber,
                    i.VendorName,
                    i.Amount,
                    i.InvoiceDate,
                    i.DueDate,
                    i.Status,
                    p.ProjectCode,
                    p.Name AS ProjectName,
                    i.AiConfidenceScore,
                    i.CreatedAt,
                    i.UpdatedAt
                FROM Invoices i
                LEFT JOIN Projects p ON i.FinalProjectKey = p.NetvisorProjectKey
                WHERE (@StatusFilter IS NULL OR i.Status = @StatusFilter)
                  AND (@SearchTerm IS NULL OR
                       i.InvoiceNumber LIKE @SearchPattern OR
                       i.VendorName LIKE @SearchPattern)
                ORDER BY i.CreatedAt DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);
            command.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
            command.Parameters.AddWithValue("@SearchPattern", searchTerm != null ? $"%{EscapeLikePattern(searchTerm)}%" : DBNull.Value);
            command.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                invoices.Add(new InvoiceDto
                {
                    Id = reader.GetInt32(0),
                    NetvisorInvoiceKey = reader.GetInt32(1),
                    InvoiceNumber = reader.GetString(2),
                    VendorName = reader.GetString(3),
                    Amount = reader.GetDecimal(4),
                    InvoiceDate = reader.GetDateTime(5),
                    DueDate = reader.GetDateTime(6),
                    Status = reader.GetString(7),
                    ProjectCode = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ProjectName = reader.IsDBNull(9) ? null : reader.GetString(9),
                    AiConfidenceScore = reader.IsDBNull(10) ? null : reader.GetDouble(10),
                    CreatedAt = reader.GetDateTime(11),
                    UpdatedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe laskujen haussa");
        }

        return invoices;
    }

    /// <summary>
    /// Get total count of invoices
    /// </summary>
    public async Task<int> GetInvoiceCountAsync(string? statusFilter = null, string? searchTerm = null)
    {
        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException("Connection string not set");

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT COUNT(*)
                FROM Invoices
                WHERE (@StatusFilter IS NULL OR Status = @StatusFilter)
                  AND (@SearchTerm IS NULL OR
                       InvoiceNumber LIKE @SearchPattern OR
                       VendorName LIKE @SearchPattern)";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);
            command.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
            command.Parameters.AddWithValue("@SearchPattern", searchTerm != null ? $"%{EscapeLikePattern(searchTerm)}%" : DBNull.Value);

            var count = await command.ExecuteScalarAsync();
            return count != null ? Convert.ToInt32(count) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Virhe laskujen määrän haussa");
            return 0;
        }
    }

    /// <summary>
    /// Escape LIKE pattern special characters to prevent SQL injection
    /// </summary>
    private static string EscapeLikePattern(string pattern)
    {
        return pattern
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }
}
