using System.Text.Json;
using Dapper;
using AtlasRFID.Manager.Api.Data;

namespace AtlasRFID.Manager.Api.Services
{
    public interface IAuditLogger
    {
        Task WriteAsync(
            Guid? companyId,
            Guid? userId,
            string action,
            string entityType,
            Guid? entityId,
            object before = null,
            object after = null,
            string message = null,
            string source = "API",
            string correlationId = null);
    }

    public class AuditLogger : IAuditLogger
    {
        private readonly DbConnectionFactory _factory;

        public AuditLogger(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task WriteAsync(
            Guid? companyId,
            Guid? userId,
            string action,
            string entityType,
            Guid? entityId,
            object before = null,
            object after = null,
            string message = null,
            string source = "API",
            string correlationId = null)
        {
            using var conn = _factory.Create();

            var id = Guid.NewGuid();

            var detailsObj = new
            {
                action,
                entityType,
                entityId,
                before,
                after
            };

            var detailsJson = JsonSerializer.Serialize(detailsObj, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var msg = message ?? $"{action} {entityType} {entityId}";

            const string sql = @"
INSERT INTO dbo.Logs (Id, CompanyId, Category, Level, Source, Message, Details, CorrelationId, CreatedAt, CreatedByUserId)
VALUES (@Id, @CompanyId, 'Audit', 'Info', @Source, @Message, @Details, @CorrelationId, SYSUTCDATETIME(), @UserId);
";

            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                CompanyId = companyId,
                Source = source,
                Message = msg,
                Details = detailsJson,
                CorrelationId = correlationId,
                UserId = userId
            });
        }
    }
}
