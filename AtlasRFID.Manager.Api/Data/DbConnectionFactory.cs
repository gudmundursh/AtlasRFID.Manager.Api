using Microsoft.Data.SqlClient;

namespace AtlasRFID.Manager.Api.Data
{
    public class DbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default");
        }

        public SqlConnection Create()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
