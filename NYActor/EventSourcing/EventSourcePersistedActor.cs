using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace NYActor.EventSourcing;

public abstract class EventSourcePersistedActor<TState> : EventSourceActor<TState>
    where TState : class, IApplicable, new()
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;
    private readonly JsonSerializerSettings _jsonSerializerSettings;

    protected EventSourcePersistedActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider)
    {
        _eventSourcePersistenceProvider = eventSourcePersistenceProvider;

        _jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        _jsonSerializerSettings.Converters.Add(new StringEnumConverter(true));
    }

    protected virtual byte[] SerializeEvent<TEvent>(TEvent @event)
    {
        return Encoding.UTF8.GetBytes(
            JsonConvert.SerializeObject(
                @event,
                _jsonSerializerSettings
            )
        );
    }

    protected virtual object DeserializeEvent(EventSourceEventContainer eventContainer)
    {
        var json = Encoding.UTF8.GetString(eventContainer.Event);

        var type = Type.GetType(eventContainer.EventType);

        if (type == null)
        {
            return null;
        }

        var @event = JsonConvert.DeserializeObject(json, type);

        return @event;
    }

    protected override async Task ApplyMultipleAsync<TEvent>(IEnumerable<TEvent> events)
    {
        var materializedEvents = events.ToList();

        if (!materializedEvents.Any()) return;

        await _eventSourcePersistenceProvider.PersistEventsAsync<TEvent>(
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
