using NYActor.EventSourcing;

namespace NYActor.Patterns.Watchdog;

public abstract class WatchableActor : Actor, IWatchdogClient
{
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
