namespace NYActor;

public interface IActorDispatcherInternal<out TActor> : IActorDispatcherInternal where TActor : IActor
{
    Task SendAsync<TMessage>(
        TMessage message,
        ActorExecutionContext actorExecutionContext = null
    );

    Task<TResult> InvokeAsync<TResult>(
        Func<TActor, Task<TResult>> req,
        string callName,
        ActorExecutionContext actorExecutionContext = null
    );

    Task InvokeAsync(
        Func<TActor, Task> req,
        string callName,
        ActorExecutionContext actorExecutionContext = null
    );
}

public interface IActorDispatcherInternal
{
    ActorNode ActorNode { get; }
    void DelayDeactivation(TimeSpan deactivationTimeout);
}
