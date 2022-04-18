namespace NYActor.EventSourcing;

public abstract class EventSourceActor<TState> : Actor
    where TState : class, IApplicable, new()
{
    protected EventSourceActor()
    {
        State = new TState();
        Version = -1;
    }

    protected Task ApplySingleAsync<TEvent>(TEvent @event) where TEvent : class =>
        ApplyMultipleAsync(Enumerable.Repeat(@event, 1));

    protected virtual Task ApplyMultipleAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : class
    {
        var materializedEvents = events.ToList();

        if (!materializedEvents.Any()) return Task.CompletedTask;

        return OnEventsApplied(materializedEvents);
    }

    protected virtual Task OnEventsApplied<TEvent>(List<TEvent> materializedEvents) where TEvent : class
    {
        foreach (var @event in materializedEvents)
        {
            State.Apply(@event);
            Version++;
        }

        return Task.CompletedTask;
    }

    protected TState State { get; }
    protected long Version { get; private set; }
}
