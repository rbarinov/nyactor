namespace NYActor.Cluster;

[LocalActorNodeActor]
public class ClusterDiscoveryActor<TActor> : Actor
    where TActor : Actor
{
    // @todo implement something more complex (e.g. remote addr / node info , etc)
    public async Task<ClusterActorDiscoveryResult> GetActorDiscoveryResult()
    {
        var actorType = typeof(TActor).Name;
        var actorKey = Key;
        await Task.Yield();

        var discovery = new ClusterActorDiscoveryResult(
            true
        );

        return discovery;
    }
}
