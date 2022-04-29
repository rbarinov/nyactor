using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace NYActor.EventSourcing;

public abstract class EventSourcePersistedActor<TState> : EventSourceActor<TState>
    where TState : class, IApplicable, new()
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;

    protected EventSourcePersistedActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider)
    {
        _eventSourcePersistenceProvider = eventSourcePersistenceProvider;
    }

    protected virtual EventSourceEventData SerializeEvent(object @event)
    {
        return new EventSourceEventData(
            $"{@event.GetType().FullName},{@event.GetType().Assembly.GetName().Name}",
            Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(
                    @event,
                    JsonSerializerConfig.Settings
                )
            )
        );
    }

    protected virtual object DeserializeEvent(EventSourceEventContainer eventContainer)
    {
        var json = Encoding.UTF8.GetString(eventContainer.EventData.Event);

        var type = Type.GetType(eventContainer.EventData.EventType);

        if (type == null)
        {
            return null;
        }

        var @event = JsonConvert.DeserializeObject(json, type);

        return @event;
    }

    protected override async Task ApplyMultipleAsync(IEnumerable<object> events)
    {
        var materializedEvents = events.ToList();

        if (!materializedEvents.Any()) return;

        await _eventSourcePersistenceProvider.PersistEventsAsync(
                GetType(),
                Key,
                Version,
                materializedEvents.Select(SerializeEvent)
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
                    Observable.FromAsync(() => OnActivationEventsApplied(Enumerable.Repeat(e, 1)))
            )
            .Merge(1)
            .IgnoreElements()
            .DefaultIfEmpty()
            .ToTask()
            .ConfigureAwait(false);
    }

    protected virtual Task OnActivationEventsApplied(IEnumerable<EventSourceEventContainer> events)
    {
        return base.OnEventsApplied(
            events.Select(DeserializeEvent)
                .ToList()
        );
    }
}
