using System.Linq.Expressions;

namespace NYActor.Cluster;

[LocalActorNodeActor]
public class ClusterLocalProxyActor<TActor> : Actor
    where TActor : Actor
{
    public Task Send<TMessage>(TMessage message)
    {
        throw new NotImplementedException();
    }

    public Task<TResult> Proxy<TResult>(
        IActorSystem localActorNode,
        string key,
        Expression<Func<TActor, Task<TResult>>> req,
        ActorExecutionContext actorExecutionContext
    )
    {
        return localActorNode.GetActor<TActor>(key)
            .InvokeAsync(req, this.ActorExecutionContext());
    }

    public Task Proxy(
        IActorSystem localActorNode,
        string key,
        Expression<Func<TActor, Task>> req,
        ActorExecutionContext actorExecutionContext
    )
    {
        return localActorNode.GetActor<TActor>(key)
            .InvokeAsync(req, this.ActorExecutionContext());
    }
}
