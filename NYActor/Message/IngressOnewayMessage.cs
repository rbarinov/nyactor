namespace NYActor.Message;

public sealed class IngressOnewayMessage : IngressMessage
{
    public IngressOnewayMessage(object payload, ActorExecutionContext actorExecutionContext)
        : base(actorExecutionContext)
    {
        Payload = payload;
    }

    public object Payload { get; }
}
