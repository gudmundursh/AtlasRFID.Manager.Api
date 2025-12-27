using AtlasRFID.Manager.Api.Data;
using AtlasRFID.Manager.Api.Models;
using Dapper;

namespace AtlasRFID.Manager.Api.Repositories
{
    public class RbacRepository
    {
        private readonly DbConnectionFactory _factory;

        public RbacRepository(DbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<RbacPermission>> GetPermissionsAsync()
        {
            using var conn = _factory.Create();
            const string sql = @"SELECT Id, Code, Name, Description, IsActive FROM dbo.Permissions WHERE IsActive = 1 ORDER BY Code;";
            return await conn.QueryAsync<RbacPermission>(sql);
        }

        public async Task<IEnumerable<RbacRole>> GetCompanyRolesAsync(Guid companyId)
        {
            using var conn = _factory.Create();
            const string sql = @"
                SELECT Id, CompanyId, Code, Name, Description, IsSystemRole, IsActive
                FROM dbo.Roles
                WHERE CompanyId = @CompanyId AND IsActive = 1
                ORDER BY Name;";
            return await conn.QueryAsync<RbacRole>(sql, new { CompanyId = companyId });
        }

        public async Task<RbacRole?> GetRoleByIdAsync(Guid roleId)
        {
            using var conn = _factory.Create();
            const string sql = @"
                SELECT TOP 1 Id, CompanyId, Code, Name, Description, IsSystemRole, IsActive
                FROM dbo.Roles
                WHERE Id = @Id;";
            return await conn.QuerySingleOrDefaultAsync<RbacRole>(sql, new { Id = roleId });
        }

        public async Task<Guid> CreateRoleAsync(Guid companyId, string code, string name, string? description, Guid createdByUserId)
        {
            using var conn = _factory.Create();
            var id = Guid.NewGuid();

            const string sql = @"
                INSERT INTO dbo.Roles
                (Id, CompanyId, Code, Name, Description, IsSystemRole, IsActive, CreatedAt, CreatedByUserId)
                VALUES
                (@Id, @CompanyId, @Code, @Name, @Description, 0, 1, SYSUTCDATETIME(), @CreatedByUserId);";

            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                CompanyId = companyId,
                Code = code,
                Name = name,
                Description = description,
                CreatedByUserId = createdByUserId
            });

            return id;
        }

        public async Task UpdateRoleAsync(Guid roleId, Guid companyId, string name, string? description, bool isActive, Guid updatedByUserId)
        {
            using var conn = _factory.Create();

            const string sql = @"
                UPDATE dbo.Roles
                SET Name = @Name,
                    Description = @Description,
                    IsActive = @IsActive,
                    UpdatedAt = SYSUTCDATETIME(),
                    UpdatedByUserId = @UpdatedByUserId
                WHERE Id = @Id AND CompanyId = @CompanyId AND IsSystemRole = 0;";

            await conn.ExecuteAsync(sql, new
            {
                Id = roleId,
                CompanyId = companyId,
                Name = name,
                Description = description,
                IsActive = isActive ? 1 : 0,
                UpdatedByUserId = updatedByUserId
            });
        }

        public async Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            using var conn = _factory.Create();

            // wipe + insert (simple + reliable)
            await conn.ExecuteAsync("DELETE FROM dbo.RolePermissions WHERE RoleId = @RoleId;", new { RoleId = roleId });

            const string insertSql = @"
                INSERT INTO dbo.RolePermissions(RoleId, PermissionId)
                VALUES (@RoleId, @PermissionId);";

            foreach (var pid in permissionIds.Distinct())
            {
                await conn.ExecuteAsync(insertSql, new { RoleId = roleId, PermissionId = pid });
            }
        }

        public async Task<IEnumerable<string>> GetRolePermissionCodesAsync(Guid roleId)
        {
            using var conn = _factory.Create();
            const string sql = @"
                SELECT p.Code
                FROM dbo.RolePermissions rp
                JOIN dbo.Permissions p ON p.Id = rp.PermissionId
                WHERE rp.RoleId = @RoleId
                ORDER BY p.Code;";
            return await conn.QueryAsync<string>(sql, new { RoleId = roleId });
        }

        public async Task<Guid> AssignRoleAsync(Guid userId, Guid roleId, string? scopeType, Guid? scopeId, Guid createdByUserId)
        {
            using var conn = _factory.Create();
            var id = Guid.NewGuid();

            const string sql = @"
                INSERT INTO dbo.UserRoleAssignments
                (Id, UserId, RoleId, ScopeType, ScopeId, IsActive, CreatedAt, CreatedByUserId)
                VALUES
                (@Id, @UserId, @RoleId, @ScopeType, @ScopeId, 1, SYSUTCDATETIME(), @CreatedByUserId);";

            await conn.ExecuteAsync(sql, new
            {
                Id = id,
                UserId = userId,
                RoleId = roleId,
                ScopeType = scopeType,
                ScopeId = scopeId,
                CreatedByUserId = createdByUserId
            });

            return id;
        }

        public async Task RemoveAssignmentAsync(Guid assignmentId)
        {
            using var conn = _factory.Create();
            await conn.ExecuteAsync("DELETE FROM dbo.UserRoleAssignments WHERE Id = @Id;", new { Id = assignmentId });
        }

        public async Task<IEnumerable<dynamic>> GetAssignmentsForUserAsync(Guid userId)
        {
            using var conn = _factory.Create();
            const string sql = @"
                SELECT
                    ura.Id,
                    ura.UserId,
                    ura.RoleId,
                    ura.ScopeType,
                    ura.ScopeId,
                    ura.IsActive,
                    r.Code AS RoleCode,
                    r.Name AS RoleName
                FROM dbo.UserRoleAssignments ura
                JOIN dbo.Roles r ON r.Id = ura.RoleId
                WHERE ura.UserId = @UserId
                ORDER BY r.Name;";
            return await conn.QueryAsync(sql, new { UserId = userId });
        }
        public async Task<Dictionary<string, Guid>> GetPermissionIdsByCodeAsync(IEnumerable<string> codes)
        {
            using var conn = _factory.Create();

            var codeList = codes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codeList.Count == 0) return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            const string sql = @"
                SELECT Code, Id
                FROM dbo.Permissions
                WHERE IsActive = 1 AND Code IN @Codes;";

            var rows = await conn.QueryAsync<(string Code, Guid Id)>(sql, new { Codes = codeList });

            return rows.ToDictionary(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);
        }

    }
}
