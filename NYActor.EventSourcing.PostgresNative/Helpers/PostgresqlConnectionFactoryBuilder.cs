using System.Data;
using Npgsql;

namespace NYActor.EventSourcing.PostgresqlNative.Helpers;

public class PostgresqlConnectionFactoryBuilder
{
    private string _connectionString = null;
    private bool _useDataSourceBuilder = false;
    private Action<NpgsqlDataSourceBuilder> _configureDataSource = null;

    public PostgresqlConnectionFactoryBuilder SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;

        return this;
    }

    public PostgresqlConnectionFactoryBuilder WithDataSourceBuilder(Action<NpgsqlDataSourceBuilder> configureDataSource)
    {
        _useDataSourceBuilder = true;
        _configureDataSource = configureDataSource;

        return this;
    }

    public IPostgresqlConnectionFactory Build()
    {
        if (!_useDataSourceBuilder)
        {
            return new SimpleConnectionFactory(_connectionString);
        }

        return new DataSourceBuilderFactory(_connectionString, _configureDataSource);
    }

    private class DataSourceBuilderFactory : IPostgresqlConnectionFactory
    {
        private readonly NpgsqlDataSourceBuilder _dataSourceBuilder;
        private NpgsqlDataSource _npgsqlDataSource;

        public DataSourceBuilderFactory(string connectionString, Action<NpgsqlDataSourceBuilder> configureDataSource)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            configureDataSource?.Invoke(dataSourceBuilder);

            _dataSourceBuilder = dataSourceBuilder;
            _npgsqlDataSource = _dataSourceBuilder.Build();

        }

        public async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var openConnectionAsync = await _npgsqlDataSource
                .OpenConnectionAsync();

            return openConnectionAsync;
        }
    }

    private class SimpleConnectionFactory : IPostgresqlConnectionFactory
    {
        private readonly string _connectionString;

        public SimpleConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var db = new NpgsqlConnection(_connectionString);
            await db.OpenAsync();

            return db;
        }
    }
}
