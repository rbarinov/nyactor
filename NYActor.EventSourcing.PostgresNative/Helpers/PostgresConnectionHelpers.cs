using Npgsql;

namespace NYActor.EventSourcing.PostgresqlNative.Helpers;

public static class PostgresConnectionHelpers
{
    public static PostgresqlCommandBuilder CommandBuilder(this NpgsqlConnection db)
    {
        return new PostgresqlCommandBuilder(db);
    }
}
