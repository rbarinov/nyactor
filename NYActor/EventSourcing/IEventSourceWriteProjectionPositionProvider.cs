namespace NYActor.EventSourcing;

public interface IEventSourceWriteProjectionPositionProvider
{
    Task<EventSourceWriteProjectionState> ReadPositionAsync(Type eventSourceWriteProjectionType);

    Task WritePositionAsync(
        Type eventSourceWriteProjectionType,
        string syncPosition
    );
}
