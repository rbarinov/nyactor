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

    public Task PersistEventsAsync<TEvent>(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<byte[]> events
    )
    {
        return _eventSourcePersistenceProvider.PersistEventsAsync<TEvent>(_type, _key, expectedVersion, events);
    }

    public IObservable<EventSourceEventContainer> ObservePersistedEvents(Type eventSourcePersistedActorType, string key)
    {
        return _eventSourcePersistenceProvider.ObservePersistedEvents(_type, _key);
    }

    public IObservable<EventSourceEventContainer> ObserveAllEvents(string fromPosition)
    {
        return _eventSourcePersistenceProvider.ObserveAllEvents(fromPosition);
    }
}