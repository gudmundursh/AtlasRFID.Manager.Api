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
        public async Task<dynamic> GetByIdForAdminAsync(Guid id)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT
                Id,
                CompanyId,
                UserName,
                Email,
                DisplayName,
                IsSuperAdmin,
                IsCompanyAdmin,
                IsActive,
                FailedLoginCount,
                LockoutUntil,
                LastLoginAt,
                CreatedAt,
                CreatedByUserId,
                UpdatedAt,
                UpdatedByUserId,
                IsDeleted
                FROM Users
                WHERE Id = @Id AND IsDeleted = 0;
            ";
            return await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
        }
        public async Task UpdateAsync(
            Guid id,
            string email,
            string displayName,
            bool isSuperAdmin,
            bool isCompanyAdmin,
            bool isActive,
            Guid updatedByUserId)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                UPDATE Users
                SET Email = @Email,
                DisplayName = @DisplayName,
                IsSuperAdmin = @IsSuperAdmin,
                IsCompanyAdmin = @IsCompanyAdmin,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id AND IsDeleted = 0;
            ";
            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                Email = email,
                DisplayName = displayName,
                IsSuperAdmin = isSuperAdmin ? 1 : 0,
                IsCompanyAdmin = isCompanyAdmin ? 1 : 0,
                IsActive = isActive ? 1 : 0,
                UpdatedByUserId = updatedByUserId
            });
        }
        public async Task SetActiveAsync(Guid id, bool isActive, Guid updatedByUserId)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                UPDATE Users
                SET IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id AND IsDeleted = 0;
            ";
            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                IsActive = isActive ? 1 : 0,
                UpdatedByUserId = updatedByUserId
            });
        }
        public async Task ResetPasswordAsync(Guid id, string passwordHash, Guid updatedByUserId)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                UPDATE Users
                SET PasswordHash = @PasswordHash,
                PasswordUpdatedAt = SYSUTCDATETIME(),
                FailedLoginCount = 0,
                LockoutUntil = NULL,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id AND IsDeleted = 0;
            ";

            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                PasswordHash = passwordHash,
                UpdatedByUserId = updatedByUserId
            });
        }
        public async Task<(Guid? CompanyId, bool IsSuperAdmin, string UserName)?> GetSecurityInfoAsync(Guid id)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT TOP 1 CompanyId, IsSuperAdmin, UserName
                FROM Users
                WHERE Id = @Id AND IsDeleted = 0;
            ";

            var row = await connection.QuerySingleOrDefaultAsync(sql, new { Id = id });
            if (row == null) return null;

            Guid? companyId = row.CompanyId;
            bool isSuperAdmin = row.IsSuperAdmin == true;
            string userName = row.UserName;

            return (companyId, isSuperAdmin, userName);
        }
        public async Task<IEnumerable<dynamic>> GetByCompanyAsync(Guid companyId)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT
                Id,
                CompanyId,
                UserName,
                Email,
                DisplayName,
                IsSuperAdmin,
                IsCompanyAdmin,
                IsActive,
                FailedLoginCount,
                LockoutUntil,
                LastLoginAt,
                CreatedAt,
                CreatedByUserId,
                UpdatedAt,
                UpdatedByUserId
                FROM Users
                WHERE CompanyId = @CompanyId
                AND IsDeleted = 0
                ORDER BY UserName;
                ";
            return await connection.QueryAsync(sql, new { CompanyId = companyId });
        }
        public async Task UpdateTenantAsync(
            Guid id,
            Guid companyId,
            string email,
            string displayName,
            bool isCompanyAdmin,
            bool isActive,
            Guid updatedByUserId)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                UPDATE Users
                SET Email = @Email,
                DisplayName = @DisplayName,
                IsCompanyAdmin = @IsCompanyAdmin,
                IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id
                AND CompanyId = @CompanyId
                AND IsDeleted = 0
                AND IsSuperAdmin = 0; -- tenant admins can never edit superadmins
            ";
            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                CompanyId = companyId,
                Email = email,
                DisplayName = displayName,
                IsCompanyAdmin = isCompanyAdmin ? 1 : 0,
                IsActive = isActive ? 1 : 0,
                UpdatedByUserId = updatedByUserId
            });
        }
        public async Task SetActiveTenantAsync(Guid id, Guid companyId, bool isActive, Guid updatedByUserId)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                UPDATE Users
                SET IsActive = @IsActive,
                UpdatedAt = SYSUTCDATETIME(),
                UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id
                AND CompanyId = @CompanyId
                AND IsDeleted = 0
                AND IsSuperAdmin = 0;
            ";
            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                CompanyId = companyId,
                IsActive = isActive ? 1 : 0,
                UpdatedByUserId = updatedByUserId
            });
        }


    }
}
