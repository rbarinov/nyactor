namespace NYActor.Cluster.ClusterNodeDiscovery.Events;

public class ClusterNodeDiscoveryCompletedEvent
{
    public ClusterNodeDiscoveryCompletedEvent(
        DateTime eventAt,
        string providerName,
        IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo> nodes
    )
    {
        EventAt = eventAt;
        ProviderName = providerName;
        Nodes = nodes;
    }

    public DateTime EventAt { get; }
    public string ProviderName { get; }
    public IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo> Nodes { get; }
}
