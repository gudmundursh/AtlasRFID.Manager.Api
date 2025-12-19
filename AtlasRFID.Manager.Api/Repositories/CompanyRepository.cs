using Dapper;
using AtlasRFID.Manager.Api.Data;
using AtlasRFID.Manager.Api.Models;

namespace AtlasRFID.Manager.Api.Repositories
{
    public class CompanyRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public CompanyRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Company>> GetAllAsync()
        {
            using var connection = _connectionFactory.Create();

            const string sql = @"
                SELECT Id, Name, IsActive
                FROM Companies
                ORDER BY Name
            ";

            return await connection.QueryAsync<Company>(sql);
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


    }
}
