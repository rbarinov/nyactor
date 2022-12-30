using Npgsql;

namespace NYActor.EventSourcing.PostgresqlNative.Helpers;

public class PostgresqlCommandBuilder
{
    private readonly NpgsqlConnection _db;
    private string _command;
    private List<(string parameterName, object parameterValue, string dbType)> _params;

    public PostgresqlCommandBuilder(NpgsqlConnection db)
    {
        _db = db;
    }

    public PostgresqlCommandBuilder SetCommand(string command)
    {
        _command = command;

        return this;
    }

    public PostgresqlCommandBuilder AppendParameter<T>(string parameterName, T parameterValue, string dbType = null)
    {
        _params ??= new();
        _params.Add((parameterName, parameterValue, dbType));

        return this;
    }

    public NpgsqlCommand Build()
    {
        var command = new NpgsqlCommand();
        command.Connection = _db;
        command.CommandText = _command;

        if (_params != null)
        {
            foreach (var (parameterName, parameterValue, dataTypeName) in _params)
            {
                var npgsqlParameter = new NpgsqlParameter(
                    parameterName,
                    parameterValue
                );

                if (dataTypeName != null)
                {
                    npgsqlParameter.DataTypeName = dataTypeName!;
                }

                command.Parameters.Add(npgsqlParameter);
            }
        }

        return command;
    }
}
