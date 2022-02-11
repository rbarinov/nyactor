namespace NYActor.Cluster.ClusterNodeDiscovery;

public class ClusterNodeDiscoveryInfo
{
    public ClusterNodeDiscoveryInfo(
        DateTime completedAt,
        string providerName,
        IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo> nodes
    )
    {
        CompletedAt = completedAt;
        ProviderName = providerName;
        Nodes = nodes;
    }

    public DateTime CompletedAt { get; }
    public string ProviderName { get; }
    public IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo> Nodes { get; }
}
