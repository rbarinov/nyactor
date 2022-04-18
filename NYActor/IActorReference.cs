using System.Linq.Expressions;

namespace NYActor;

public interface IActorReference<TActor> : IActorReference where TActor : IActor
{
    Task SendAsync<TMessage>(
        TMessage message,
        ActorExecutionContext actorExecutionContext = null
    );

    Task<TResult> InvokeAsync<TResult>(
        Expression<Func<TActor, Task<TResult>>> req,
        ActorExecutionContext actorExecutionContext = null
    );

    Task InvokeAsync(
        Expression<Func<TActor, Task>> req,
        ActorExecutionContext actorExecutionContext = null
    );
}

public interface IActorReference
{
    IActorReference<TBaseActor> ToBaseRef<TBaseActor>() where TBaseActor : IActor;
}
