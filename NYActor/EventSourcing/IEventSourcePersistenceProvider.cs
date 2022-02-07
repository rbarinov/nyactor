namespace NYActor.EventSourcing;

public interface IEventSourcePersistenceProvider
{
    Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long exceptedVersion,
        IEnumerable<object> events
    );

    IObservable<EventSourceEventContainer> ObservePersistedEvents(
        Type eventSourcePersistedActorType,
        string key
    );

    IObservable<EventSourceEventContainer> ObserveAllEvents(string fromPosition);
}
