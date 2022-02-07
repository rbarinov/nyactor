using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace NYActor.EventSourcing;

public abstract class EventSourcePersistedActor<TState> : Actor
    where TState : class, IApplicable, new()
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;

    protected EventSourcePersistedActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider)
    {
        State = new TState();

        _eventSourcePersistenceProvider = eventSourcePersistenceProvider;
        Version = -1;
    }

    protected TState State { get; }
    protected long Version { get; private set; }

    protected Task ApplySingleAsync<TEvent>(TEvent @event) where TEvent : class =>
        ApplyMultipleAsync(Enumerable.Repeat(@event, 1));

    protected async Task ApplyMultipleAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : class
    {
        var materializedEvents = events.ToList();

        if (!materializedEvents.Any()) return;

        await _eventSourcePersistenceProvider.PersistEventsAsync(
                GetType(),
                Key,
                Version,
                materializedEvents
            )
            .ConfigureAwait(false);

        foreach (var @event in materializedEvents)
        {
            State.Apply(@event);
            Version++;
        }
    }

    protected override async Task OnActivated()
    {
        await base.OnActivated()
            .ConfigureAwait(false);

        await _eventSourcePersistenceProvider.ObservePersistedEvents(GetType(), Key)
            .Do(
                e =>
                {
                    State.Apply(e.Event);
                    Version++;
                }
            )
            .IgnoreElements()
            .DefaultIfEmpty()
            .ToTask()
            .ConfigureAwait(false);
    }
}
