using System.Collections.ObjectModel;

namespace NYActor.Cluster.ClusterNodeDiscovery.Discovery;

public class StaticClusterNodeDiscoveryProvider : IClusterNodeDiscoveryProvider
{
    private readonly IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo> _nodes;

    public StaticClusterNodeDiscoveryProvider(TimeSpan discoveryInterval, params ClusterNodeDiscoveryNodeInfo[] nodes)
    {
        DiscoveryInterval = discoveryInterval;

        _nodes = nodes?.ToList()
            .AsReadOnly();
    }

    public TimeSpan DiscoveryInterval { get; }

    public Task<IReadOnlyCollection<ClusterNodeDiscoveryNodeInfo>> DiscoverAsync()
    {
        return Task.FromResult(_nodes);
    }
}
