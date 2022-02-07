namespace NYActor.EventSourcing;

public interface IEventSourcePersistenceProvider
{
    Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        long exceptedVersion,
        IEnumerable<object> events
    );

    IObservable<EventSourceEventContainer> ObservePersistedEvents(
        Type eventSourcePersistedActorType
    );

    IObservable<EventSourceEventContainer> ObserveAllEvents(string fromPosition);
}
