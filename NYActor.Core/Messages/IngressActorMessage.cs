namespace NYActor.Core
{
    public class IngressActorMessage : ActorMessage
    {
        public object Payload { get; }

        public IngressActorMessage(object payload)
        {
            Payload = payload;
        }
    }
}