namespace NYActor.Cluster.ClusterNodeDiscovery;

public class ClusterNodeDiscoveryNodeInfo
{
    public ClusterNodeDiscoveryNodeInfo(
        string address,
        int port
    )
    {
        Address = address;
        Port = port;
    }

    public string Address { get; }
    public int Port { get; }
}
