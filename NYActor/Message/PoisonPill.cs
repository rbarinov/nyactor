namespace NYActor.Message;

public sealed class PoisonPill : ActorMessage
{
    public static readonly PoisonPill Default = new();
}
