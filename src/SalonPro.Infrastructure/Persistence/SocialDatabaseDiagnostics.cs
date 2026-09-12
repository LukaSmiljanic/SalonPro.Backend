using System.Data;
using Microsoft.EntityFrameworkCore;
using SalonPro.Application.Common.Interfaces;

namespace SalonPro.Infrastructure.Persistence;

public class SocialDatabaseDiagnostics : ISocialDatabaseDiagnostics
{
    private readonly ApplicationDbContext _context;

    private static readonly string[] RequiredSocialPostColumns =
    [
        "Id", "TenantId", "ScheduledAt", "Topic", "Caption", "Hashtags",
        "ImagePrompt", "ImageUrl", "InstagramMediaId", "Status",
        "PublishedAt", "FailureReason", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy",
    ];

    public SocialDatabaseDiagnostics(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SocialDbDiagnosticsDto> InspectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var databaseName = await ScalarStringAsync(connection, "SELECT DB_NAME()", cancellationToken) ?? "";
        var serverName = connection.DataSource;

        var socialPostsExists = await TableExistsAsync(connection, "SocialPosts", cancellationToken);
        var igExists = await TableExistsAsync(connection, "TenantInstagramAccounts", cancellationToken);

        var socialColumns = socialPostsExists
            ? await ListColumnsAsync(connection, "SocialPosts", cancellationToken)
            : [];
        var igColumns = igExists
            ? await ListColumnsAsync(connection, "TenantInstagramAccounts", cancellationToken)
            : [];

        int? rowCount = null;
        string? postsError = null;
        if (socialPostsExists)
        {
            try
            {
                rowCount = await ScalarIntAsync(connection, "SELECT COUNT(*) FROM SocialPosts", cancellationToken);
            }
            catch (Exception ex)
            {
                postsError = ex.Message;
            }
        }
        else
        {
            postsError = "Tabela SocialPosts ne postoji u bazi koju API koristi.";
        }

        var missing = RequiredSocialPostColumns
            .Where(c => !socialColumns.Contains(c, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (missing.Count > 0)
            postsError = (postsError == null ? "" : postsError + " ") +
                         $"Nedostaju kolone: {string.Join(", ", missing)}";

        string? igError = null;
        if (!igExists)
            igError = "Tabela TenantInstagramAccounts ne postoji.";

        return new SocialDbDiagnosticsDto(
            databaseName,
            serverName,
            socialPostsExists,
            igExists,
            socialColumns,
            igColumns,
            rowCount,
            postsError,
            igError);
    }

    private static async Task<bool> TableExistsAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = @name AND TABLE_SCHEMA = 'dbo'
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@name";
        p.Value = tableName;
        cmd.Parameters.Add(p);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    private static async Task<List<string>> ListColumnsAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @name AND TABLE_SCHEMA = 'dbo'
            ORDER BY ORDINAL_POSITION
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@name";
        p.Value = tableName;
        cmd.Parameters.Add(p);

        var list = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            list.Add(reader.GetString(0));
        return list;
    }

    private static async Task<string?> ScalarStringAsync(
        System.Data.Common.DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    private static async Task<int> ScalarIntAsync(
        System.Data.Common.DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
    }
}
