namespace NYActor.Message;

public class IngressMessage : ActorMessage
{
    protected IngressMessage(ActorExecutionContext actorExecutionContext)
    {
        ActorExecutionContext = actorExecutionContext;
    }

    public ActorExecutionContext ActorExecutionContext { get; }
}
