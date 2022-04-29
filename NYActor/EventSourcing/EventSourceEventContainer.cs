namespace NYActor.EventSourcing;

public class EventSourceEventContainer
{
    public string Position { get; }
    public EventSourceEventData EventData { get; }

    public EventSourceEventContainer(string position, EventSourceEventData eventData)
    {
        Position = position;
        EventData = eventData;
    }
}

public class EventSourceEventData
{
    public string EventType { get; }
    public byte[] Event { get; }

    public EventSourceEventData(string eventType, byte[] @event)
    {
        EventType = eventType;
        Event = @event;
    }
}
