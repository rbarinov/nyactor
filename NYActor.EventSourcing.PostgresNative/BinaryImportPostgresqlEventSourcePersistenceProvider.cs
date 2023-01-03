using System.Reactive.Disposables;
using Npgsql;
using NYActor.EventSourcing.PostgresqlNative.Helpers;

namespace NYActor.EventSourcing.PostgresqlNative;

public class BinaryImportPostgresqlEventSourcePersistenceProvider
    : PostgresqlEventSourcePersistenceProvider
{
    private NpgsqlBinaryImporter _writer;
    private Dictionary<string, long> _versionInc;
    private long _globalVersionInc;
    private SemaphoreSlim _sema;

    public BinaryImportPostgresqlEventSourcePersistenceProvider(string connectionString, string prefix)
        : base(connectionString, prefix)
    {
    }

    public async Task<IAsyncDisposable> OpenBinaryImportScope()
    {
        _sema = new SemaphoreSlim(1);
        var db = await ConnectionFactory.OpenConnectionAsync();
        var tran = await db.BeginTransactionAsync();

        _globalVersionInc = 0;
        _versionInc = new Dictionary<string, long>();

        var createTable = db.CommandBuilder()
            .SetCommand(
                @"-- noinspection SqlNoDataSourceInspectionForFile
create temp table tmp_events
(
    stream_id          varchar(1024) not null,
    version_inc        bigint        not null,
    global_version_inc bigint        not null,
    type               varchar(512),
    payload            bytea,
    primary key (stream_id, version_inc)
) on commit drop;
"
            )
            .Build();

        await createTable.ExecuteNonQueryAsync();

        _writer = await db.BeginBinaryImportAsync(
            $@"
copy tmp_events (
    stream_id,
    version_inc,
    global_version_inc,
    type,
    payload
)
from stdin (format binary)"
        );

        return new AsyncDisposable(
            async () =>
            {
                await _writer.CompleteAsync();
                await _writer.CloseAsync();

                await using var complete = db.CommandBuilder()
                    .SetCommand(
                        $@"-- noinspection SqlNoDataSourceInspectionForFile
insert into {Prefix}_events (stream_id, version, global_version, type, payload)
select e.stream_id, coalesce(de.version, 0) + e.version_inc as version, nextval('{Prefix}_global_version'), e.type, e.payload
from tmp_events e
         left join {Prefix}_streams de on e.stream_id = de.stream_id
order by e.global_version_inc;

insert into {Prefix}_streams (stream_id, version)
select e.stream_id, max(coalesce(de.version, 0) + e.version_inc) as version
from tmp_events e
         left join {Prefix}_streams de on e.stream_id = de.stream_id
group by e.stream_id
on conflict (stream_id) do update set version = excluded.version;
"
                    )
                    .Build();

                await complete.ExecuteNonQueryAsync();

                await tran.CommitAsync();

                await db.DisposeAsync();

                _sema.Dispose();
            }
        );
    }

    public override async Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<EventSourceEventData> events
    )
    {
        var streamId = GetStreamKey(eventSourcePersistedActorType, key);

        await _sema.WaitAsync();

        try
        {
            var streamVersionInc = _versionInc.TryGetValue(streamId, out var v) ? v : 0;

            foreach (var ev in events)
            {
                await _writer.StartRowAsync();

                await _writer.WriteSafeAsync(streamId);
                await _writer.WriteSafeAsync(expectedVersion + 1 + streamVersionInc++);
                await _writer.WriteSafeAsync(_globalVersionInc++);
                await _writer.WriteSafeAsync(ev.EventType);
                await _writer.WriteSafeAsync(ev.Event);
            }

            _versionInc[streamId] = streamVersionInc;
        }
        finally
        {
            _sema.Release();
        }
    }

    private class AsyncDisposable : IAsyncDisposable
    {
        private readonly Func<Task> _dispose;

        public AsyncDisposable(Func<Task> dispose)
        {
            _dispose = dispose;
        }

        public async ValueTask DisposeAsync() =>
            await _dispose();
    }
}
