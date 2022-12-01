namespace NYActor.Cluster;

public class ClusterActorSystemBuilder : ActorSystemBuilder
{
    public override IActorSystem Build(IServiceProvider serviceProvider)
    {
        var clusterActorNode = new ClusterActorNode(
            serviceProvider,
            ActorDeactivationTimeout,
            TracingActivityFactory
        );

        return clusterActorNode;
    }
}
