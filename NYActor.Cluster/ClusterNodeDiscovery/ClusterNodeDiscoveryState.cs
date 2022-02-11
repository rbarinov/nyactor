using NYActor.Cluster.ClusterNodeDiscovery.Events;
using NYActor.EventSourcing;

namespace NYActor.Cluster.ClusterNodeDiscovery;

public class ClusterNodeDiscoveryState : IApplicable
{
    public ClusterNodeDiscoveryInfo Current { get; private set; }

    public void Apply(object ev)
    {
        if (ev is ClusterNodeDiscoveryCompletedEvent discoveryCompletedEvent)
            Current = new ClusterNodeDiscoveryInfo(
                discoveryCompletedEvent.EventAt,
                discoveryCompletedEvent.ProviderName,
                discoveryCompletedEvent.Nodes
            );
    }
}
