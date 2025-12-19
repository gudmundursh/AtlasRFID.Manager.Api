using AtlasRFID.Manager.Api.Data;
using AtlasRFID.Manager.Api.Models;
using Dapper;

namespace AtlasRFID.Manager.Api.Repositories
{
    public class UserRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public UserRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<UserAuthRow> FindForLoginAsync(string userNameOrEmail)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT TOP 1
                    Id,
                    CompanyId,
                    UserName,
                    Email,
                    PasswordHash,
                    IsSuperAdmin,
                    IsCompanyAdmin,
                    IsActive,
                    IsDeleted,
                    ISNULL(FailedLoginCount, 0) AS FailedLoginCount,
                    LockoutUntil
                FROM Users
                WHERE (UserName = @v OR Email = @v)
                  AND IsDeleted = 0
            ";

            return await connection.QuerySingleOrDefaultAsync<UserAuthRow>(sql, new { v = userNameOrEmail });
        }

        public async Task RegisterFailedLoginAsync(Guid userId, int newFailedCount, DateTime? lockoutUntil)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                UPDATE Users
                SET FailedLoginCount = @FailedLoginCount,
                    LockoutUntil = @LockoutUntil
                WHERE Id = @Id
            ";

            await connection.ExecuteAsync(sql, new { Id = userId, FailedLoginCount = newFailedCount, LockoutUntil = lockoutUntil });
        }

        public async Task RegisterSuccessfulLoginAsync(Guid userId)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                UPDATE Users
                SET FailedLoginCount = 0,
                    LockoutUntil = NULL,
                    LastLoginAt = SYSUTCDATETIME()
                WHERE Id = @Id
            ";

            await connection.ExecuteAsync(sql, new { Id = userId });
        }
    }
}
