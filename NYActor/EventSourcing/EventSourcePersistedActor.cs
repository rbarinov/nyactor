using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace NYActor.EventSourcing;

public abstract class EventSourcePersistedActor<TState> : EventSourceActor<TState>
    where TState : class, IApplicable, new()
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;

    protected EventSourcePersistedActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider)
    {
        _eventSourcePersistenceProvider = eventSourcePersistenceProvider;
    }

    protected override async Task ApplyMultipleAsync<TEvent>(IEnumerable<TEvent> events)
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

        await base.ApplyMultipleAsync(materializedEvents);
    }

    protected override async Task OnActivated()
    {
        await base.OnActivated()
            .ConfigureAwait(false);

        await _eventSourcePersistenceProvider.ObservePersistedEvents(GetType(), Key)
            .Select(
                e =>
                    Observable.FromAsync(() => base.ApplyMultipleAsync(Enumerable.Repeat(e.Event, 1)))
            )
            .Merge(1)
            .IgnoreElements()
            .DefaultIfEmpty()
            .ToTask()
            .ConfigureAwait(false);
    }
}