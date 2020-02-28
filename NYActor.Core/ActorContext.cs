namespace NYActor.Core
{
    public class ActorContext : IActorContext
    {
        public ActorContext(IActorWrapper self, IActorSystem actorSystem)
        {
            Self = self;
            System = actorSystem;
        }

        public  IActorWrapper Self { get; }
        public IActorSystem System { get; }
    }
}