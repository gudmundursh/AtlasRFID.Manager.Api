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
    }
}
