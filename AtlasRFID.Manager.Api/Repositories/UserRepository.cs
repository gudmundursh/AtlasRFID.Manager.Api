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
        public async Task<Guid> CreateAsync(
            Guid? companyId,
            string userName,
            string email,
            string displayName,
            string passwordHash,
            bool isSuperAdmin,
            bool isCompanyAdmin,
            bool isActive,
            Guid createdByUserId)
        {
            using var connection = _connectionFactory.Create();

            var id = Guid.NewGuid();

            const string sql = @"
            INSERT INTO Users
            (
            Id, CompanyId, UserName, Email, DisplayName,
            PasswordHash, PasswordUpdatedAt,
            IsSuperAdmin, IsCompanyAdmin, IsActive,
            FailedLoginCount, IsDeleted,
            CreatedAt, CreatedByUserId
            )
            VALUES
            (
            @Id, @CompanyId, @UserName, @Email, @DisplayName,
            @PasswordHash, SYSUTCDATETIME(),
            @IsSuperAdmin, @IsCompanyAdmin, @IsActive,
            0, 0,
            SYSUTCDATETIME(), @CreatedByUserId
            );
            ";

            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                CompanyId = companyId,
                UserName = userName,
                Email = email,
                DisplayName = displayName,
                PasswordHash = passwordHash,
                IsSuperAdmin = isSuperAdmin ? 1 : 0,
                IsCompanyAdmin = isCompanyAdmin ? 1 : 0,
                IsActive = isActive ? 1 : 0,
                CreatedByUserId = createdByUserId
            });

            return id;
        }

        public async Task<bool> UserNameOrEmailExistsAsync(string userName, string email)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
        SELECT CASE WHEN EXISTS(
            SELECT 1 FROM Users
            WHERE IsDeleted = 0 AND (UserName = @UserName OR Email = @Email)
        ) THEN 1 ELSE 0 END;
    ";

            return await connection.ExecuteScalarAsync<int>(sql, new { UserName = userName, Email = email }) == 1;
        }

    }
}
