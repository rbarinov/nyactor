using Npgsql;

namespace NYActor.EventSourcing.Projections.Postgres;

public class PostgresConnectionFactoryBuilder
{
    private string _connectionString;

    private class PostgresConnectionFactory : IPostgresConnectionFactory
    {
        private readonly string _connectionString;

        public PostgresConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<NpgsqlConnection> OpenConnection()
        {
            var db = new NpgsqlConnection(_connectionString);

            await db.OpenAsync();

            return db;
        }
    }

    public PostgresConnectionFactoryBuilder WithConnectionString(string connectionString)
    {
        _connectionString = connectionString;

        return this;
    }

    public IPostgresConnectionFactory Build()
    {
        return new PostgresConnectionFactory(_connectionString);
    }
}
