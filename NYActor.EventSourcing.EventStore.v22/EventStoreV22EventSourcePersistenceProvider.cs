using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using EventStore.Client;

namespace NYActor.EventSourcing.EventStore.v22;

public class EventStoreV22EventSourcePersistenceProvider :
    IEventStoreV22EventSourcePersistenceProvider
{
    private readonly EventStoreClient _eventStoreClient;
    private readonly int _activationEventReadBatchSize;

    public EventStoreV22EventSourcePersistenceProvider(
        EventStoreClient eventStoreClient,
        int activationEventReadBatchSize
    )
    {
        _eventStoreClient = eventStoreClient;
        _activationEventReadBatchSize = activationEventReadBatchSize;
    }

    public async Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<EventSourceEventData> events
    )
    {
        var eventStoreEvents = events
            .Select(
                e => new EventData(
                    Uuid.NewUuid(),
                    e.EventType,
                    e.Event,
                    null
                )
            )
            .ToList();

        var stream = GetStreamName(eventSourcePersistedActorType, key);

        try
        {
            await _eventStoreClient.AppendToStreamAsync(
                    stream,
                    StreamRevision.FromInt64(expectedVersion),
                    eventStoreEvents
                )
                .ConfigureAwait(false);
        }
        catch (WrongExpectedVersionException e)
        {
            if (e.ActualVersion != expectedVersion + eventStoreEvents.Count)
            {
                throw;
            }
        }
    }

    protected virtual string GetStreamName(Type eventSourcePersistedActorType, string key)
    {
        return $"{eventSourcePersistedActorType.FullName}-{key}";
    }

    public IObservable<EventSourceEventContainer> ObservePersistedEvents(
        Type eventSourcePersistedActorType,
        string key
    )
    {
        return Observable.Create<ResolvedEvent>(
                async observer =>
                {
                    ulong pos = StreamPosition.Start;

                    try
                    {
                        do
                        {
                            var batch = _eventStoreClient.ReadStreamAsync(
                                Direction.Forwards,
                                GetStreamName(eventSourcePersistedActorType, key),
                                pos,
                                _activationEventReadBatchSize,
                                false
                            );

                            var events = await batch.ToListAsync();

                            foreach (var ev in events)
                            {
                                observer.OnNext(ev);
                                pos++;
                            }

                            if (events.Count < _activationEventReadBatchSize) break;
                        } while (true);

                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                }
            )
            .Select(
                e =>
                {
                    var position = $"{e.OriginalPosition?.CommitPosition}-{e.OriginalPosition?.PreparePosition}";

                    return new EventSourceEventContainer(
                        position,
                        new EventSourceEventData(e.Event.EventType, e.Event.Data.ToArray())
                    );
                }
            );
    }

    public IObservable<EventSourceEventContainer> ObserveAllEvents(
        string fromPosition,
        Action<EventSourceSubscriptionCatchUp> catchupSubscription = null
    )
    {
        return Observable.Create<EventSourceEventContainer>(
            observer =>
            {
                var pattern = @"(\d+)-(\d+)";

                var positionState = new BehaviorSubject<(ulong commitPosition, ulong preparePosition)>(
                    (Position.Start.CommitPosition, Position.Start.PreparePosition)
                );

                if (!string.IsNullOrEmpty(fromPosition) && Regex.IsMatch(fromPosition, pattern))
                {
                    var match = Regex.Match(fromPosition, pattern);

                    var startCommitPosition = Convert.ToUInt64(
                        match.Groups[1]
                            .Value
                    );

                    var startPreparePosition = Convert.ToUInt64(
                        match.Groups[1]
                            .Value
                    );

                    positionState.OnNext((startCommitPosition, startPreparePosition));
                }

                IDisposable eventStoreSubscription = null;
                var isDisposing = false;

                void SubscribeToEventStore(ulong commitPosition, ulong preparePosition)
                {
                    var pos = new Position(commitPosition, preparePosition);

                    Task.Run(
                        async () =>
                        {
                            try
                            {
                                while (!isDisposing)
                                {
                                    var maxCount = _activationEventReadBatchSize;

                                    var read = _eventStoreClient.ReadAllAsync(
                                        Direction.Forwards,
                                        pos,
                                        maxCount,
                                        false
                                    );

                                    var events = await read.ToListAsync();

                                    foreach (var ev in events)
                                    {
                                        var currentCommitPosition = ev.OriginalPosition?.CommitPosition ?? default;
                                        var currentPreparePosition = ev.OriginalPosition?.PreparePosition ?? default;

                                        observer.OnNext(
                                            new EventSourceEventContainer(
                                                $"{currentCommitPosition}-{currentPreparePosition}",
                                                new EventSourceEventData(ev.Event.EventType, ev.Event.Data.ToArray())
                                            )
                                        );

                                        positionState.OnNext((currentCommitPosition, currentPreparePosition));

                                        pos = ev.Event.Position;
                                    }

                                    if (events.Count < maxCount) break;
                                }

                                if (isDisposing)
                                {
                                    return;
                                }

                                catchupSubscription?.Invoke(
                                    new EventSourceSubscriptionCatchUp(
                                        true,
                                        string.Empty,
                                        string.Empty
                                    )
                                );

                                if (isDisposing)
                                {
                                    return;
                                }

                                eventStoreSubscription = await _eventStoreClient.SubscribeToAllAsync(
                                    FromAll.After(pos),
                                    (sub, ev, ct) =>
                                    {
                                        var currentCommitPosition = ev.OriginalPosition?.CommitPosition ?? default;
                                        var currentPreparePosition = ev.OriginalPosition?.PreparePosition ?? default;

                                        observer.OnNext(
                                            new EventSourceEventContainer(
                                                $"{currentCommitPosition}-{currentPreparePosition}",
                                                new EventSourceEventData(ev.Event.EventType, ev.Event.Data.ToArray())
                                            )
                                        );

                                        positionState.OnNext((currentCommitPosition, currentPreparePosition));

                                        return Task.CompletedTask;
                                    },
                                    false,
                                    (sub, reason, ex) =>
                                    {
                                        if (!isDisposing)
                                        {
                                            SubscribeToEventStore(
                                                positionState.Value.commitPosition,
                                                positionState.Value.preparePosition
                                            );
                                        }
                                    }
                                );
                            }
                            catch (Exception ex)
                            {
                                if (!isDisposing)
                                {
                                    SubscribeToEventStore(
                                        positionState.Value.commitPosition,
                                        positionState.Value.preparePosition
                                    );
                                }
                            }
                        }
                    );
                }

                SubscribeToEventStore(positionState.Value.commitPosition, positionState.Value.preparePosition);

                var disposable = Disposable.Create(
                    () =>
                    {
                        isDisposing = true;
                        eventStoreSubscription?.Dispose();
                    }
                );

                return disposable;
            }
        );
    }
}
