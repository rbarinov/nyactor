namespace NYActor.EventSourcing;

public class EventSourceEventContainer
{
    public string Position { get; }
    public string EventType { get; }
    public byte[] Event { get; }

    public EventSourceEventContainer(string position, string eventType, byte[] @event)
    {
        Position = position;
        EventType = eventType;
        Event = @event;
    }
}
