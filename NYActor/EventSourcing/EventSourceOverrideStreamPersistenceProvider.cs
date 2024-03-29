namespace NYActor.EventSourcing;

public class EventSourceOverrideStreamPersistenceProvider : IEventSourcePersistenceProvider
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;
    private readonly Type _type;
    private readonly string _key;

    public EventSourceOverrideStreamPersistenceProvider(
        IEventSourcePersistenceProvider eventSourcePersistenceProvider,
        Type type,
        string key
    )
    {
        _eventSourcePersistenceProvider = eventSourcePersistenceProvider;
        _type = type;
        _key = key;
    }

    public Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<EventSourceEventData> events
    )
    {
        return _eventSourcePersistenceProvider.PersistEventsAsync(_type, _key, expectedVersion, events);
    }

    public IObservable<EventSourceEventContainer> ObservePersistedEvents(Type eventSourcePersistedActorType, string key)
    {
        return _eventSourcePersistenceProvider.ObservePersistedEvents(_type, _key);
    }

    public IObservable<EventSourceEventContainer> ObserveAllEvents(
        string fromPosition,
        Action<EventSourceSubscriptionCatchUp> catchupSubscription = null
    )
    {
        return _eventSourcePersistenceProvider.ObserveAllEvents(fromPosition, catchupSubscription);
    }
}