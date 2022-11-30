using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NYActor.EventSourcing;

public class BufferedEventSourcePersistenceProvider : IEventSourcePersistenceProvider
{
    private readonly IEventSourcePersistenceProvider _persistenceProvider;
    private readonly bool _skipReadStreams;

    private readonly Subject<(Type eventSourcePersistedActorType, string key, long expectedVersion,
        IEnumerable<EventSourceEventData> events)> _buffer;

    private Subject<Unit> _completion;

    public BufferedEventSourcePersistenceProvider(
        IEventSourcePersistenceProvider persistenceProvider,
        bool skipReadStreams = false
    )
    {
        _persistenceProvider = persistenceProvider;
        _skipReadStreams = skipReadStreams;

        _buffer = new Subject<(Type eventSourcePersistedActorType, string key,
            long expectedVersion,
            IEnumerable<EventSourceEventData> events)>();

        _completion = new Subject<Unit>();

        _buffer
            .Buffer(TimeSpan.FromMilliseconds(1000), 1000)
            .Where(e => e.Any())
            .Select(
                chunk => chunk
                    .GroupBy(e => (e.eventSourcePersistedActorType, e.key))
                    .Select(
                        chunk =>
                        {
                            var eventSourcePersistedActorType = chunk.First()
                                .eventSourcePersistedActorType;

                            var key = chunk.First()
                                .key;

                            var expectedVersion = chunk.First()
                                .expectedVersion;

                            var events = chunk
                                .SelectMany(e => e.events)
                                .ToList();

                            return (
                                eventSourcePersistedActorType,
                                key,
                                expectedVersion,
                                events
                            );
                        }
                    )
                    .Select(
                        e => Observable.FromAsync(
                            () => persistenceProvider.PersistEventsAsync(
                                e.eventSourcePersistedActorType,
                                e.key,
                                e.expectedVersion,
                                e.events
                            )
                        )
                    )
                    .Merge()
            )
            .Merge(1)
            .LastOrDefaultAsync()
            .Subscribe(_completion);
    }

    public async Task AwaitCompletionAsync()
    {
        _buffer.OnCompleted();

        await _completion.LastOrDefaultAsync();
    }

    public Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<EventSourceEventData> events
    )
    {
        _buffer.OnNext((eventSourcePersistedActorType, key, expectedVersion, events.ToList()));

        return Task.CompletedTask;
    }

    public IObservable<EventSourceEventContainer> ObservePersistedEvents(Type eventSourcePersistedActorType, string key)
    {
        return _skipReadStreams
            ? Observable.Empty<EventSourceEventContainer>()
            : _persistenceProvider.ObservePersistedEvents(eventSourcePersistedActorType, key);
    }

    public IObservable<EventSourceEventContainer> ObserveAllEvents(
        string fromPosition,
        Action<EventSourceSubscriptionCatchUp> catchupSubscription = null
    )
    {
        return _skipReadStreams
            ? Observable.Empty<EventSourceEventContainer>()
            : _persistenceProvider.ObserveAllEvents(fromPosition, catchupSubscription);
    }
}
