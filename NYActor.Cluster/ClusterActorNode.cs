using System.Reflection;

namespace NYActor.Cluster;

public class ClusterActorNode : IActorSystem
{
    private LocalActorNode _localActorNode;

    public ClusterActorNode(
        IServiceProvider serviceProvider,
        TimeSpan actorDeactivationTimeout,
        Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
            tracingActivity)> tracingActivityFactory
    )
    {
        var clusterActorSystem = this;

        _localActorNode = new LocalActorNode(
            serviceProvider,
            actorDeactivationTimeout,
            tracingActivityFactory,
            () => clusterActorSystem
        );
    }

    public void Dispose()
    {
        _localActorNode.Dispose();
    }

    public IActorReference<TActor> GetActor<TActor>(string key) where TActor : Actor
    {
        var localActorNodeActorAttribute = typeof(TActor).GetCustomAttribute<LocalActorNodeActorAttribute>();

        if (localActorNodeActorAttribute != null)
        {
            return _localActorNode.GetActor<TActor>(key);
        }

        // 1. make a proxy actor reference
        // 2. proxy talks to local discovery actor
        // 3. proxy wraps a call to local or remote via a remote proxy actor
        return new ClusterActorReference<TActor, TActor>(this, _localActorNode, key);
    }
}
