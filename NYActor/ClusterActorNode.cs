using System.Linq.Expressions;

namespace NYActor;

public class ClusterActorNode : LocalActorNode
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

public class ClusterActorReference<TActor> : IActorReference<TActor>
    where TActor : IActor
{
    private readonly IActorSystem _actorSystem;
    private readonly string _key;

    public ClusterActorReference(IActorSystem actorSystem, string key)
    {
        _actorSystem = actorSystem;
        _key = key;
    }

    public IActorReference<TBaseActor> ToBaseRef<TBaseActor>() where TBaseActor : IActor
    {
        throw new NotImplementedException();
    }

    public Task SendAsync<TMessage>(TMessage message, ActorExecutionContext actorExecutionContext = null)
    {
        throw new NotImplementedException();
    }

    public Task<TResult> InvokeAsync<TResult>(
        Expression<Func<TActor, Task<TResult>>> req,
        ActorExecutionContext actorExecutionContext = null
    )
    {
        throw new NotImplementedException();
    }

    public Task InvokeAsync(Expression<Func<TActor, Task>> req, ActorExecutionContext actorExecutionContext = null)
    {
        throw new NotImplementedException();
    }
}
