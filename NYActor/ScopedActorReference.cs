using System.Linq.Expressions;

namespace NYActor;

public class ScopedActorReference<TActor> : IActorReference<TActor> where TActor : IActor
{
    private readonly IActorReference<TActor> _actorReference;
    private readonly ScopedExecutionContext _scopedExecutionContext;

    public ScopedActorReference(
        IActorReference<TActor> actorReference,
        ScopedExecutionContext scopedExecutionContext
    )
    {
        _actorReference = actorReference;
        _scopedExecutionContext = scopedExecutionContext;
    }

    public Task SendAsync<TMessage>(TMessage message, ActorExecutionContext actorExecutionContext = null)
    {
        return _actorReference.SendAsync(message, GetScopedExecutionContext(actorExecutionContext));
    }

    public Task<TResult> InvokeAsync<TResult>(
        Expression<Func<TActor, Task<TResult>>> req,
        ActorExecutionContext actorExecutionContext = null
    )
    {
        return _actorReference.InvokeAsync(req, GetScopedExecutionContext(actorExecutionContext));
    }

    public Task InvokeAsync(Expression<Func<TActor, Task>> req, ActorExecutionContext actorExecutionContext = null)
    {
        return _actorReference.InvokeAsync(req, GetScopedExecutionContext(actorExecutionContext));
    }

    public IActorReference<TBaseActor> ToBaseRef<TBaseActor>() where TBaseActor : IActor
    {
        var baseActorRef = _actorReference.ToBaseRef<TBaseActor>();

        return new ScopedActorReference<TBaseActor>(baseActorRef, _scopedExecutionContext);
    }

    private ActorExecutionContext GetScopedExecutionContext(ActorExecutionContext actorExecutionContext)
    {
        if (actorExecutionContext != ActorExecutionContext.Empty && _scopedExecutionContext != null)
            return new ScopedExecutionContext(
                new Dictionary<string, string>(_scopedExecutionContext?.Scope)
            );

        return ActorExecutionContext.Empty;
    }
}
