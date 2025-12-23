using AtlasRFID.Manager.Api.Data;
using AtlasRFID.Manager.Api.Models;
using AtlasRFID.Manager.Api.Security;
using Dapper;

namespace AtlasRFID.Manager.Api.Repositories
{
    public class CompanyRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        private readonly ITenantProvider _tenant;

        public CompanyRepository(
            DbConnectionFactory connectionFactory,
            ITenantProvider tenant)
        {
            _connectionFactory = connectionFactory;
            _tenant = tenant;
        }


        public CompanyRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Company>> GetAllAsync()
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT Id, Code, Name, IsActive
                FROM Companies
                WHERE Id = @CompanyId
                ";

            return await connection.QueryAsync<Company>(
                sql,
                new { CompanyId = _tenant.GetCompanyId() }
            );
        }
        public async Task<Guid> CreateAsync(string code, string name)
        {
            using var connection = _connectionFactory.Create();

            var id = Guid.NewGuid();

            const string sql = @"
                INSERT INTO Companies (Id, Code, Name, IsActive)
                VALUES (@Id, @Code, @Name, 1);
                ";

            await connection.ExecuteAsync(sql, new { Id = id, Code = code, Name = name });   //ExecuteAsync is for commands (INSERT/UPDATE/DELETE).
            return id;
        }
        public async Task UpdateAsync(Guid id, string name, bool isActive)
        {
            using var conn = _connectionFactory.Create();

            const string sql = @"
                UPDATE dbo.Companies
                SET Name = @Name,
                IsActive = @IsActive
                WHERE Id = @Id;
            ";
            await conn.ExecuteAsync(sql, new { Id = id, Name = name, IsActive = isActive });
        }
        public async Task SetActiveAsync(Guid id, bool isActive)
        {
            using var conn = _connectionFactory.Create();

            const string sql = @"UPDATE dbo.Companies SET IsActive = @IsActive WHERE Id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = id, IsActive = isActive });
        }

        public async Task<Company> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT Id, Name, IsActive
                FROM Companies
                WHERE Id = @Id;
                ";

            return await connection.QuerySingleOrDefaultAsync<Company>(sql, new { Id = id });
        }
        public async Task<IEnumerable<Company>> GetAllSystemAsync()
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT Id, Code, Name, IsActive
                FROM Companies
                ORDER BY Name
            ";

            return await connection.QueryAsync<Company>(sql);
        }



    }
}
