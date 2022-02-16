using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

        protected override async Task OnActivationEventsApplied<TEvent>(IEnumerable<TEvent> events)
        {
            await base.OnActivationEventsApplied(events);

            await _throttledActor.OnEventsApplied(events.ToList());
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
            .Subscribe(e => _closer.OnNext(Unit.Default));
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

    protected override async Task ApplyMultipleAsync<TEvent>(IEnumerable<TEvent> events)
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
}