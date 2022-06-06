namespace NYActor.EventSourcing;

public class EventSourceEvent
{
    public string Position { get; }
    public string EventType { get; }
    public object Event { get; }

    public EventSourceEvent(string position, string eventType, object @event)
    {
        Position = position;
        EventType = eventType;
        Event = @event;
    }
}
