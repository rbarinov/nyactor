using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;

namespace NYActor.EventSourcing.EventStore.v5;

public class EventStoreV5EventSourcePersistenceProvider :
    IEventStoreV5EventSourcePersistenceProvider
{
    private readonly IEventStoreConnection _eventStoreConnection;
    private readonly int _activationEventReadBatchSize;

    public EventStoreV5EventSourcePersistenceProvider(
        IEventStoreConnection eventStoreConnection,
        int activationEventReadBatchSize
    )
    {
        _eventStoreConnection = eventStoreConnection;
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
                    Guid.NewGuid(),
                    e.EventType,
                    true,
                    e.Event,
                    null
                )
            )
            .ToList();

        var stream = GetStreamName(eventSourcePersistedActorType, key);

        try
        {
            await _eventStoreConnection.AppendToStreamAsync(stream, expectedVersion, eventStoreEvents)
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
                    var read = 0;

                    try
                    {
                        do
                        {
                            var batch = await _eventStoreConnection.ReadStreamEventsForwardAsync(
                                GetStreamName(eventSourcePersistedActorType, key),
                                read,
                                _activationEventReadBatchSize,
                                false
                            );

                            foreach (var @event in batch.Events)
                            {
                                observer.OnNext(@event);
                            }

                            read += batch.Events.Length;

                            if (batch.IsEndOfStream) break;
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
                        new EventSourceEventData(e.Event.EventType, e.Event.Data)
                    );
                }
            );
    }

    public IObservable<EventSourceEventContainer> ObserveAllEvents(string fromPosition)
    {
        return Observable.Create<EventSourceEventContainer>(
            observer =>
            {
                var pattern = @"(\d+)-(\d+)";

                var positionState = new BehaviorSubject<(long commitPosition, long preparePosition)>(
                    (Position.Start.CommitPosition, Position.Start.PreparePosition)
                );

                if (!string.IsNullOrEmpty(fromPosition) && Regex.IsMatch(fromPosition, pattern))
                {
                    var match = Regex.Match(fromPosition, pattern);

                    var startCommitPosition = Convert.ToInt64(
                        match.Groups[1]
                            .Value
                    );

                    var startPreparePosition = Convert.ToInt64(
                        match.Groups[1]
                            .Value
                    );

                    positionState.OnNext((startCommitPosition, startPreparePosition));
                }

                EventStoreAllCatchUpSubscription eventStoreSubscription;
                var isDisposing = false;

                void SubscribeToEventStore(long commitPosition, long preparePosition)
                {
                    eventStoreSubscription = _eventStoreConnection.SubscribeToAllFrom(
                        new Position(commitPosition, preparePosition),
                        new CatchUpSubscriptionSettings(512, 512, false, false),
                        eventAppeared: (subscription, ese) =>
                        {
                            var currentCommitPosition = ese.OriginalPosition?.CommitPosition ?? default;
                            var currentPreparePosition = ese.OriginalPosition?.PreparePosition ?? default;

                            observer.OnNext(
                                new EventSourceEventContainer(
                                    $"{currentCommitPosition}-{currentPreparePosition}",
                                    new EventSourceEventData(ese.Event.EventType, ese.Event.Data)
                                )
                            );

                            positionState.OnNext((currentCommitPosition, currentPreparePosition));
                        },
                        liveProcessingStarted: catchup => { },
                        subscriptionDropped: (sub, res, ex) =>
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

                SubscribeToEventStore(positionState.Value.commitPosition, positionState.Value.preparePosition);

                var disposable = Disposable.Create(
                    () =>
                    {
                        isDisposing = true;
                        eventStoreSubscription?.Stop();
                    }
                );

                return disposable;
            }
        );
    }
}
