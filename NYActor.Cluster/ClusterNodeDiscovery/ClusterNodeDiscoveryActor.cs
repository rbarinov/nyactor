using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NYActor.Cluster.ClusterNodeDiscovery.Events;
using NYActor.EventSourcing;

namespace NYActor.Cluster.ClusterNodeDiscovery;

[LocalActorNodeActor]
public class ClusterNodeDiscoveryActor : EventSourceActor<ClusterNodeDiscoveryState>
{
    private readonly IClusterNodeDiscoveryProvider _clusterNodeDiscoveryProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly Subject<Unit> _unsubscribeAll = new();

    public ClusterNodeDiscoveryActor(
        IClusterNodeDiscoveryProvider clusterNodeDiscoveryProvider,
        ITimeProvider timeProvider
    )
    {
        _clusterNodeDiscoveryProvider = clusterNodeDiscoveryProvider;
        _timeProvider = timeProvider;
    }

    protected override async Task OnActivated()
    {
        await base.OnActivated();

        this.EnableDeactivationDelay(_unsubscribeAll);

        Observable.FromAsync(() => _clusterNodeDiscoveryProvider.DiscoverAsync())
            .Where(e => e != null)
            .Select(
                e => Observable.FromAsync(
                    () => this.Self()
                        .InvokeAsync(c => c.WriteDiscoveryResultAsync(e), ActorExecutionContext.Empty)
                )
            )
            .Merge(1)
            .RepeatAfterDelay(_clusterNodeDiscoveryProvider.DiscoveryInterval)
            .TakeUntil(_unsubscribeAll)
            .Subscribe();
    }

    private async Task WriteDiscoveryResultAsync(IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo> nodes)
    {
        await ApplySingleAsync(
            new ClusterNodeDiscoveryCompletedEvent(
                _timeProvider.UtcNow,
                _clusterNodeDiscoveryProvider.GetType()
                    .FullName,
                nodes
            )
        );
    }

    public Task<ClusterNodeDiscoveryInfo> GetInfo()
    {
        return Task.FromResult(State.Current);
    }

    protected override async Task OnDeactivated()
    {
        _unsubscribeAll.OnNext(Unit.Default);
        _unsubscribeAll.OnCompleted();

        await base.OnDeactivated();
    }
}
