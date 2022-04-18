using NYActor.EventSourcing;

namespace NYActor.Patterns.Watchdog;

public abstract class EventSourcePersistedWatchableActor<T> : EventSourcePersistedActor<T>, IWatchdogClient
    where T : class, IApplicable, new()
{
    protected EventSourcePersistedWatchableActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider)
        : base(eventSourcePersistenceProvider)
    {
    }

    protected abstract IActorReference<WatchdogActor> GetWatchdog();

    protected override async Task OnActivated()
    {
        await base.OnActivated();

        await GetWatchdog()
            .InvokeAsync(e => e.Watch());
    }

    protected override async Task OnDeactivated()
    {
        await GetWatchdog()
            .InvokeAsync(e => e.Unwatch());

        await base.OnDeactivated();
    }
}
