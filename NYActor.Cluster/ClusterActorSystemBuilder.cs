namespace NYActor.Cluster;

public class ClusterActorSystemBuilder : ActorSystemBuilder
{
    public override IActorSystem Build()
    {
        var clusterActorNode = new ClusterActorNode(
            ServiceProvider,
            ActorDeactivationTimeout,
            TracingActivityFactory
        );

        return clusterActorNode;
    }
}