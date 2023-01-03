using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using NYActor.EventSourcing.PostgresqlNative.Helpers;

namespace NYActor.EventSourcing.PostgresqlNative;

public class PostgresqlEventSourcePersistenceProvider : IEventSourcePersistenceProvider
{
    protected readonly string Prefix;
    protected readonly IPostgresqlConnectionFactory ConnectionFactory;

    public PostgresqlEventSourcePersistenceProvider(string connectionString, string prefix)
    {
        var prefixPattern = @"^([a-z][a-z0-9]*)$";

        if (string.IsNullOrWhiteSpace(prefix) || !Regex.IsMatch(prefix, prefixPattern))
        {
            throw new ArgumentOutOfRangeException(nameof(prefix), prefixPattern);
        }

        Prefix = prefix;

        ConnectionFactory = new PostgresqlConnectionFactoryBuilder()
            .SetConnectionString(connectionString)
            .WithDataSourceBuilder(e => e.MapComposite<EventContainerCompositeDto>($"{Prefix}_event_container"))
            .Build();
    }

    public virtual async Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<EventSourceEventData> events
    )
    {
        await using var db = await ConnectionFactory.OpenConnectionAsync();

        await using var command = db.CommandBuilder()
            .SetCommand(
                $@"-- noinspection SqlNoDataSourceInspectionForFile
call {Prefix}_append_events(@stream_id, @expected_version, @events);"
            )
            .AppendParameter(
                "stream_id",
                GetStreamKey(eventSourcePersistedActorType, key)
            )
            .AppendParameter(
                "expected_version",
                expectedVersion
            )
            .AppendParameter(
                "events",
                events.Select(
                        e => new EventContainerCompositeDto
                        {
                            Type = e.EventType,
                            Payload = e.Event
                        }
                    )
                    .ToList(),
                $"{Prefix}_event_container[]"
            )
            .Build();

        await command.ExecuteNonQueryAsync();
    }

    public IObservable<EventSourceEventContainer> ObservePersistedEvents(
        Type eventSourcePersistedActorType,
        string key
    ) =>
        Observable.Create<EventSourceEventContainer>(
            observer =>
            {
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                Task.Run(
                        async () =>
                        {
                            try
                            {
                                await using var db = await ConnectionFactory.OpenConnectionAsync();

                                await using var command = db.CommandBuilder()
                                    .SetCommand(
                                        $@"-- noinspection SqlNoDataSourceInspectionForFile
select global_version, type, payload
from {Prefix}_events
where stream_id = @stream_id
order by version asc;"
                                    )
                                    .AppendParameter("stream_id", GetStreamKey(eventSourcePersistedActorType, key))
                                    .Build();

                                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                                while (await reader.ReadAsync(cancellationToken))
                                {
                                    var globalVersion = reader.GetInt64(0);
                                    var type = reader.GetString(1);
                                    var payload = reader.GetFieldValue<byte[]>(2);

                                    observer.OnNext(
                                        new EventSourceEventContainer(
                                            globalVersion.ToString(),
                                            new EventSourceEventData(type, payload)
                                        )
                                    );
                                }

                                observer.OnCompleted();
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                            }
                        },
                        cancellationToken
                    )
                    .Ignore();

                return cts;
            }
        );

    public IObservable<EventSourceEventContainer> ObserveAllEvents(
        string fromPosition,
        Action<EventSourceSubscriptionCatchUp> catchupSubscription = null
    ) =>
        Observable.Create<EventSourceEventContainer>(
            async joinedObserver =>
            {
                var minGlobalPosition = long.TryParse(fromPosition, out var parsed) ? parsed : -1;
                var globalPosition = new BehaviorSubject<long>(minGlobalPosition);

                var pgPosition = new BehaviorSubject<long>(-1);

                var subDb = await ConnectionFactory.OpenConnectionAsync();
                var subCts = new CancellationTokenSource();
                var subCancellationToken = subCts.Token;

                subDb.Notification += (_, args) =>
                {
                    if (long.TryParse(args.Payload, out var parsed))
                    {
                        pgPosition.OnNext(parsed);
                    }
                };

                await using var subCommand = subDb.CommandBuilder()
                    .SetCommand(
                        $@"-- noinspection SqlNoDataSourceInspectionForFile
listen {Prefix}_sub;"
                    )
                    .Build();

                await subCommand.ExecuteNonQueryAsync(subCancellationToken);

                Task.Run(
                        async () =>
                        {
                            while (!subCancellationToken.IsCancellationRequested)
                            {
                                await subDb.WaitAsync(subCancellationToken);
                            }
                        },
                        subCancellationToken
                    )
                    .Ignore();

                var disposable = ObserveAllEventsBase(() => globalPosition.Value)
                    .Do(e => globalPosition.OnNext(long.Parse(e.Position)))
                    .Concat(
                        Observable.Create<EventSourceEventContainer>(
                            localObserver =>
                            {
                                catchupSubscription?.Invoke(new EventSourceSubscriptionCatchUp(true, default, default));
                                localObserver.OnCompleted();

                                return Disposable.Empty;
                            }
                        )
                    )
                    .Concat(
                        Observable.Create<EventSourceEventContainer>(
                                localObserver =>
                                {
                                    var runnerPos = globalPosition.Value;
                                    var pgPos = pgPosition.Value;

                                    if (pgPos > runnerPos)
                                    {
                                        return ObserveAllEventsBase(() => globalPosition.Value)
                                            .Do(e => globalPosition.OnNext(long.Parse(e.Position)))
                                            .Subscribe(localObserver);
                                    }

                                    localObserver.OnCompleted();

                                    return Disposable.Empty;
                                }
                            )
                            .RepeatAfterDelay(TimeSpan.FromMilliseconds(200))
                    )
                    .Subscribe(joinedObserver);

                return Disposable.Create(
                    () =>
                    {
                        disposable.Dispose();
                        globalPosition.Dispose();
                        subCts.Cancel();
                        subCts.Dispose();
                        subDb.Dispose();
                        pgPosition.Dispose();
                    }
                );
            }
        );

    private IObservable<EventSourceEventContainer> ObserveAllEventsBase(
        Func<long> fromPositionGetter
    ) =>
        Observable.Create<EventSourceEventContainer>(
            observer =>
            {
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;

                var minGlobalVersion = fromPositionGetter();

                Task.Run(
                        async () =>
                        {
                            try
                            {
                                await using var db = await ConnectionFactory.OpenConnectionAsync();

                                await using var command = db.CommandBuilder()
                                    .SetCommand(
                                        $@"-- noinspection SqlNoDataSourceInspectionForFile
select global_version, type, payload
from {Prefix}_events
where global_version > @global_version
order by global_version asc;"
                                    )
                                    .AppendParameter(
                                        "global_version",
                                        minGlobalVersion
                                    )
                                    .Build();

                                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                                while (await reader.ReadAsync(cancellationToken))
                                {
                                    var globalVersion = reader.GetInt64(0);
                                    var type = reader.GetString(1);
                                    var payload = reader.GetFieldValue<byte[]>(2);

                                    observer.OnNext(
                                        new EventSourceEventContainer(
                                            globalVersion.ToString(),
                                            new EventSourceEventData(type, payload)
                                        )
                                    );
                                }

                                observer.OnCompleted();
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                            }
                        },
                        cancellationToken
                    )
                    .Ignore();

                return cts;
            }
        );

    protected static string GetStreamKey(Type eventSourcePersistedActorType, string key) =>
        $"{eventSourcePersistedActorType.FullName}-{key}";

    public async Task InitDbAsync()
    {
        #region sql

        var sql = @"-- noinspection SqlNoDataSourceInspectionForFile
        
        create or replace procedure init_event_store(
            prefix text
        )
            language plpgsql
        as
        $$
        begin
            execute '
            create table if not exists ' || prefix || '_streams
            (
                stream_id varchar(1024) not null primary key,
                version   int           not null
            );
            ';
        
            execute '
            create table if not exists ' || prefix || '_events
            (
                stream_id      varchar(1024) not null,
                version        bigint        not null,
                global_version bigint        not null,
                type           varchar(512),
                payload        bytea,
                primary key (stream_id, version)
            );
            ';
        
            execute '
            create sequence if not exists ' || prefix || '_global_version start 1;
            ';
        
            if not exists(select 1 from pg_type where typname = prefix || '_event_container') then
                execute '
                create type ' || prefix || '_event_container as
                (
                    type    varchar(512),
                    payload bytea
                );
                ';
            end if;
        
            execute '
            create or replace procedure ' || prefix || '_append_events(
                in_stream_id text,
                in_expected_version bigint,
                in_events ' || prefix || '_event_container[]
            )
                language plpgsql
            as
            $' || '$
            declare
                updated_count int;
                valid_version bigint;
            begin
                if in_events is null or array_length(in_events, 1) = 0 then
                    raise ''no events to append'';
                end if;
        
                if in_expected_version = -1 then
                    begin
                        insert into ' || prefix || '_streams (stream_id, version)
                        values (in_stream_id, -1 + array_length(in_events, 1));
                    exception
                        when unique_violation then
                            select version
                            into valid_version
                            from ' || prefix || '_streams
                            where stream_id = in_stream_id;
        
                            raise ''invalid expected version. current version is %'', valid_version;
                    end;
                else
                    update ' || prefix || '_streams
                    set version = in_expected_version + array_length(in_events, 1)
                    where stream_id = in_stream_id
                      and version = in_expected_version;
        
                    get diagnostics updated_count = row_count;
        
                    if updated_count <> 1 then
                        select version
                        into valid_version
                        from ' || prefix || '_streams
                        where stream_id = in_stream_id;
        
                        raise ''invalid expected version. current version is %'', coalesce(valid_version, -1);
                    end if;
                end if;
        
                insert into ' || prefix || '_events
                    (stream_id, version, global_version, type, payload)
                select in_stream_id,
                       in_expected_version::bigint + t.ordinality,
                       nextval(''' || prefix || '_global_version''),
                       t.type,
                       t.payload
                from (select ""type"", ""payload"", ordinality
                      from unnest(in_events) with ordinality) t;
        
                perform pg_notify(''' || prefix || '_sub'', currval(''' || prefix || '_global_version'')::text);
        
                commit;
            end;
            $' || '$;
            ';
        
            commit;
        end;
        $$;
        ";

        #endregion

        await using var db = await ConnectionFactory.OpenConnectionAsync();

        await using var command = db.CommandBuilder()
            .SetCommand(sql)
            .Build();

        await command.ExecuteNonQueryAsync();

        await using var commandInit = db.CommandBuilder()
            .SetCommand(
                @"-- noinspection SqlNoDataSourceInspectionForFile
call init_event_store(@prefix);"
            )
            .AppendParameter("prefix", Prefix)
            .Build();

        await commandInit.ExecuteNonQueryAsync();
    }
}
