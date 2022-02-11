namespace NYActor.Cluster.ClusterNodeDiscovery;

public interface IClusterNodeDiscoveryProvider
{
    TimeSpan DiscoveryInterval { get; }
    Task<IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo>> DiscoverAsync();
}
