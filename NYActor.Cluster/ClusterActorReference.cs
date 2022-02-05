using System.Linq.Expressions;

namespace NYActor.Cluster;

public class ClusterActorReference<TActor, TOriginalActor> : IActorReference<TActor>
    where TActor : Actor where TOriginalActor : Actor
{
    private readonly IActorSystem _actorSystem;
    private readonly LocalActorNode _localActorNode;
    private readonly string _key;

    public ClusterActorReference(IActorSystem actorSystem, LocalActorNode localActorNode, string key)
    {
        _actorSystem = actorSystem;
        _localActorNode = localActorNode;
        _key = key;
    }

    public IActorReference<TBaseActor> ToBaseRef<TBaseActor>() where TBaseActor : IActor
    {
        // ???

        // 1. make a proxy actor reference
        // 2. proxy talks to local discovery actor
        // 3. proxy wraps a call to local or remote via a remote proxy actor
        throw new NotImplementedException();
    }

    public Task SendAsync<TMessage>(TMessage message, ActorExecutionContext actorExecutionContext = null)
    {
        // 1. make a proxy actor reference
        // 2. proxy talks to local discovery actor
        // 3. proxy wraps a call to local or remote via a remote proxy actor

        throw new NotImplementedException();
    }

    public async Task<TResult> InvokeAsync<TResult>(
        Expression<Func<TActor, Task<TResult>>> req,
        ActorExecutionContext actorExecutionContext
    )
    {
        var discoveryResult = await _actorSystem.GetActor<ClusterDiscoveryActor<TActor>>(_key)
            .InvokeAsync(e => e.GetActorDiscoveryResult(), actorExecutionContext);

        // 1. make a proxy actor reference
        // 2. proxy talks to local discovery actor
        // 3. proxy wraps a call to local or remote via a remote proxy actor

        if (discoveryResult.IsLocal)
        {
            return await _actorSystem.GetActor<ClusterLocalProxyActor<TActor>>(_key)
                .InvokeAsync(
                    e => e.Proxy(_localActorNode, _key, req, actorExecutionContext),
                    actorExecutionContext
                );
        }

        throw new NotImplementedException();
    }

    public async Task InvokeAsync(
        Expression<Func<TActor, Task>> req,
        ActorExecutionContext actorExecutionContext
    )
    {
        var discoveryResult = await _actorSystem.GetActor<ClusterDiscoveryActor<TActor>>(_key)
            .InvokeAsync(e => e.GetActorDiscoveryResult(), actorExecutionContext);

        // 1. make a proxy actor reference
        // 2. proxy talks to local discovery actor
        // 3. proxy wraps a call to local or remote via a remote proxy actor

        if (discoveryResult.IsLocal)
        {
            await _actorSystem.GetActor<ClusterLocalProxyActor<TActor>>(_key)
                .InvokeAsync(
                    e => e.Proxy(_localActorNode, _key, req, actorExecutionContext),
                    actorExecutionContext
                );

            return;
        }

        throw new NotImplementedException();
    }
}
