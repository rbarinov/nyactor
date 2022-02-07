namespace NYActor.EventSourcing;

public class EventSourceEventContainer
{
    public string Position { get; }
    public object Event { get; }

    public EventSourceEventContainer(string position, object @event)
    {
        Position = position;
        Event = @event;
    }
}
