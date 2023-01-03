using Npgsql;
using NpgsqlTypes;

namespace NYActor.EventSourcing.PostgresqlNative.Helpers;

public static class PostgresHelpers
{
    public static Task WriteSafeAsync<T>(
        this NpgsqlBinaryImporter writer,
        T? value,
        CancellationToken cancellationToken = default
    )
        where T : struct
        => value.HasValue
            ? writer.WriteAsync(value.Value, cancellationToken)
            : writer.WriteNullAsync(cancellationToken);

    public static Task WriteSafeAsync<T>(
        this NpgsqlBinaryImporter writer,
        T value,
        CancellationToken cancellationToken = default
    )
        => writer.WriteAsync(value, cancellationToken);

    public static Task WriteArraySafeAsync(
        this NpgsqlBinaryImporter writer,
        List<string> value,
        CancellationToken cancellationToken = default
    )
        => writer.WriteAsync(value, NpgsqlDbType.Array | NpgsqlDbType.Text, cancellationToken);
}