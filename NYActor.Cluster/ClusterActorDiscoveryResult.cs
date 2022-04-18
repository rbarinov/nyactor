namespace NYActor.Cluster;

public class ClusterActorDiscoveryResult
{
    public bool IsLocal { get; }

    public ClusterActorDiscoveryResult(bool isLocal)
    {
        IsLocal = isLocal;
    }
}
