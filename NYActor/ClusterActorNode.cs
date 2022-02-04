namespace NYActor;

public class ClusterActorNode : ActorNode
{
    public ClusterActorNode(
        IServiceProvider serviceProvider,
        TimeSpan actorDeactivationTimeout,
        Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
            tracingActivity)> tracingActivityFactory
    )
        : base(serviceProvider, actorDeactivationTimeout, tracingActivityFactory)
    {
    }

    public override IActorReference<TActor> GetActor<TActor>(string key)
    {
        // @todo change logic for local/remote calls
        return base.GetActor<TActor>(key);
    }
}
