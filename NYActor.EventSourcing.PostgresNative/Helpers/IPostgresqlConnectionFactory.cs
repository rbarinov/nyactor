using Npgsql;

namespace NYActor.EventSourcing.PostgresqlNative.Helpers;

public interface IPostgresqlConnectionFactory
{
    Task<NpgsqlConnection> OpenConnectionAsync();
}
