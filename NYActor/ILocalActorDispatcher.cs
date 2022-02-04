namespace NYActor;

public interface ILocalActorDispatcher<out TActor> : ILocalActorDispatcher where TActor : IActor
{
    ActorExecutionContext CurrentExecutionContext { get; }

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

public interface ILocalActorDispatcher
{
    LocalActorNode LocalActorNode { get; }
    void DelayDeactivation(TimeSpan deactivationTimeout);
}
