using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NYActor.EventSourcing.PostgresqlNative.Helpers;

namespace NYActor.EventSourcing.PostgresqlNative;

public class PostgresqlEventSourcePersistenceProvider : IEventSourcePersistenceProvider
{
    private readonly IPostgresqlConnectionFactory _operationFactory;
    private readonly IPostgresqlConnectionFactory _subFactory;

    public PostgresqlEventSourcePersistenceProvider(string connectionString)
    {
        _operationFactory = new PostgresqlConnectionFactoryBuilder()
            .SetConnectionString(connectionString)
            .WithDataSourceBuilder(e => e.MapComposite<EventContainerCompositeDto>("event_container"))
            .Build();

        _subFactory = new PostgresqlConnectionFactoryBuilder()
            .SetConnectionString(connectionString)
            .Build();
    }

    public async Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<EventSourceEventData> events
    )
    {
        await using var db = await _operationFactory.OpenConnectionAsync();

        await using var command = db.CommandBuilder()
            .SetCommand(
                @"-- noinspection SqlNoDataSourceInspectionForFile
call append_events(@stream_id, @expected_version, @events);"
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
                "event_container[]"
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
                                await using var db = await _operationFactory.OpenConnectionAsync();

                                await using var command = db.CommandBuilder()
                                    .SetCommand(
                                        @"-- noinspection SqlNoDataSourceInspectionForFile
select global_version, type, payload
from events
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

                return Task.FromResult<IDisposable>(cts);
            }
        );

    public IObservable<EventSourceEventContainer> ObserveAllEvents(
        string fromPosition,
        Action<EventSourceSubscriptionCatchUp> catchupSubscription = null
    ) =>
        Observable.Create<EventSourceEventContainer>(
            async joinedObserver =>
            {
                var pgPosition = new BehaviorSubject<long>(-1);
                var globalPosition = new BehaviorSubject<long>(-1);

                var subDb = await _subFactory.OpenConnectionAsync();
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
                    .SetCommand(@"-- noinspection SqlNoDataSourceInspectionForFile
listen sub;")
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
                                await using var db = await _operationFactory.OpenConnectionAsync();

                                await using var command = db.CommandBuilder()
                                    .SetCommand(
                                        @"-- noinspection SqlNoDataSourceInspectionForFile
select global_version, type, payload
from events
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

    private static string GetStreamKey(Type eventSourcePersistedActorType, string key) =>
        $"{eventSourcePersistedActorType.FullName}-{key}";
}
