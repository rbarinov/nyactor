using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Newtonsoft.Json;
using NYActor.EventSourcing;

namespace NYActor.Patterns.Throttled;

public abstract class EventSourceThrottledActor<TState> : EventSourceActor<TState>
    where TState : class, IApplicable, new()
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;

    private class PersistedActor : EventSourcePersistedActor<TState>
    {
        private readonly EventSourceThrottledActor<TState> _throttledActor;

        public PersistedActor(
            EventSourceThrottledActor<TState> throttledActor,
            IEventSourcePersistenceProvider eventSourcePersistenceProvider
        )
            : base(
                new EventSourceOverrideStreamPersistenceProvider(
                    eventSourcePersistenceProvider,
                    throttledActor.GetType(),
                    throttledActor.Key
                )
            )
        {
            _throttledActor = throttledActor;
        }

        public async Task ApplyEvents(IEnumerable<object> events)
        {
            await ApplyMultipleAsync(events);
        }

        protected override async Task OnActivationEventsApplied(IEnumerable<EventSourceEventContainer> events)
        {
            var materializedEvents = events.ToList();

            if (!materializedEvents.Any())
            {
                return;
            }

            await base.OnActivationEventsApplied(materializedEvents);

            await _throttledActor.OnEventsApplied(
                materializedEvents.Select(DeserializeEvent)
                    .ToList()
            );
        }

        protected override EventSourceEventData SerializeEvent(object @event)
        {
            return _throttledActor.SerializeEvent(@event);
        }

        protected override object DeserializeEvent(EventSourceEventContainer eventContainer)
        {
            return _throttledActor.DeserializeEvent(eventContainer);
        }

        public TState GetState() =>
            State;

        public long GetVersion() =>
            Version;
    }

    private readonly Subject<Unit> _unsubscribe = new Subject<Unit>();
    private Subject<IEnumerable<object>> _events;
    private Subject<Unit> _closer;

    private PersistedActor _persisted;
    protected TState PersistedState => _persisted.GetState();
    protected long PersistedVersion => _persisted.GetVersion();

    protected abstract TimeSpan ThrottlingInterval { get; }

    protected EventSourceThrottledActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider)
    {
        _eventSourcePersistenceProvider = eventSourcePersistenceProvider;
    }

    protected override async Task OnActivated()
    {
        await base.OnActivated();

        _persisted = new PersistedActor(this, _eventSourcePersistenceProvider);

        _persisted.InitializeInstanceFields(Key, SelfDispatcher);

        await _persisted.Activate();

        _events = new Subject<IEnumerable<object>>();

        _closer = new Subject<Unit>();

        _events
            .Buffer(_closer)
            .Select(
                batches => Observable
                    .FromAsync(
                        () => this.Self()
                            .InvokeAsync(
                                s => s
                                    .PersistEvents(batches.SelectMany(ev => ev))
                            )
                    )
            )
            .Merge(1)
            .TakeUntil(_unsubscribe)
            .Subscribe();

        Observable
            .Interval(ThrottlingInterval)
            .TakeUntil(_unsubscribe)
            .Subscribe(_ => _closer.OnNext(Unit.Default));
    }

    private async Task PersistEvents(IEnumerable<object> events)
    {
        await _persisted.ApplyEvents(events);
    }

    protected override async Task OnDeactivated()
    {
        _unsubscribe?.OnNext(Unit.Default);
        _unsubscribe?.OnCompleted();

        await base.OnDeactivated();
    }

    protected override async Task ApplyMultipleAsync(IEnumerable<object> events)
    {
        var materializedEvents = events.ToList();

        if (!materializedEvents.Any()) return;

        await base.ApplyMultipleAsync(materializedEvents);
        _events.OnNext(materializedEvents);
    }

    public Task PersistForceAsync()
    {
        _closer.OnNext(Unit.Default);

        return Task.CompletedTask;
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
}
