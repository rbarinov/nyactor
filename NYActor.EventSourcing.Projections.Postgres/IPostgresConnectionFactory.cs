using Npgsql;

namespace NYActor.EventSourcing.Projections.Postgres;

public interface IPostgresConnectionFactory
{
    Task<NpgsqlConnection> OpenConnection();
}
