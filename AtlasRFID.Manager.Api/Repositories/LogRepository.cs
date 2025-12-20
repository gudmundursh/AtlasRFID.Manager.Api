using AtlasRFID.Manager.Api.Data;
using AtlasRFID.Manager.Api.Models;
using Dapper;

namespace AtlasRFID.Manager.Api.Repositories
{
    public class LogRepository
    {
        private readonly DbConnectionFactory _factory;

        public LogRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<LogEntry>> SearchSystemAsync(
            string category, string level, string q, DateTime? fromUtc, DateTime? toUtc, int take)
        {
            using var conn = _factory.Create();

            const string sql = @"
SELECT TOP (@Take)
    Id, CompanyId, Category, Level, Source, Message, Details, CorrelationId, CreatedAt, CreatedByUserId
FROM dbo.Logs
WHERE (@Category IS NULL OR Category = @Category)
  AND (@Level IS NULL OR Level = @Level)
  AND (@FromUtc IS NULL OR CreatedAt >= @FromUtc)
  AND (@ToUtc IS NULL OR CreatedAt <= @ToUtc)
  AND (
        @Q IS NULL OR
        Message LIKE '%' + @Q + '%' OR
        Source LIKE '%' + @Q + '%' OR
        CorrelationId LIKE '%' + @Q + '%'
      )
ORDER BY CreatedAt DESC;
";

            return await conn.QueryAsync<LogEntry>(sql, new
            {
                Category = string.IsNullOrWhiteSpace(category) ? null : category,
                Level = string.IsNullOrWhiteSpace(level) ? null : level,
                Q = string.IsNullOrWhiteSpace(q) ? null : q,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Take = Math.Clamp(take, 1, 500)
            });
        }

        public async Task<IEnumerable<LogEntry>> SearchTenantAsync(
            Guid companyId,
            string category, string level, string q, DateTime? fromUtc, DateTime? toUtc, int take)
        {
            using var conn = _factory.Create();

            const string sql = @"
SELECT TOP (@Take)
    Id, CompanyId, Category, Level, Source, Message, Details, CorrelationId, CreatedAt, CreatedByUserId
FROM dbo.Logs
WHERE CompanyId = @CompanyId
  AND (@Category IS NULL OR Category = @Category)
  AND (@Level IS NULL OR Level = @Level)
  AND (@FromUtc IS NULL OR CreatedAt >= @FromUtc)
  AND (@ToUtc IS NULL OR CreatedAt <= @ToUtc)
  AND (
        @Q IS NULL OR
        Message LIKE '%' + @Q + '%' OR
        Source LIKE '%' + @Q + '%' OR
        CorrelationId LIKE '%' + @Q + '%'
      )
ORDER BY CreatedAt DESC;
";

            return await conn.QueryAsync<LogEntry>(sql, new
            {
                CompanyId = companyId,
                Category = string.IsNullOrWhiteSpace(category) ? null : category,
                Level = string.IsNullOrWhiteSpace(level) ? null : level,
                Q = string.IsNullOrWhiteSpace(q) ? null : q,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Take = Math.Clamp(take, 1, 500)
            });
        }
    }
}
