namespace NYActor.EventSourcing;

public interface IEventSourcePersistenceProvider
{
    Task PersistEventsAsync<TEvent>(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<byte[]> events
    );

    IObservable<EventSourceEventContainer> ObservePersistedEvents(
        Type eventSourcePersistedActorType,
        string key
    );

    IObservable<EventSourceEventContainer> ObserveAllEvents(string fromPosition);
}
