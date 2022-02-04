namespace NYActor.Message;

public class IngressAskMessage : IngressMessage
{
    public IngressAskMessage(
        Func<Actor, Task<object>> invoke,
        TaskCompletionSource<object> taskCompletionSource,
        string callName,
        ActorExecutionContext actorExecutionContext
    )
        : base(actorExecutionContext)
    {
        Invoke = invoke;
        TaskCompletionSource = taskCompletionSource;
        CallName = callName;
    }

    public Func<Actor, Task<object>> Invoke { get; }
    public TaskCompletionSource<object> TaskCompletionSource { get; }
    public string CallName { get; }
}
