namespace NYActor.Core
{
    public interface IActorContext
    {
        public ActorWrapperBase Self { get; }
        public IActorSystem System { get; }
    }

    internal class ActorContext : IActorContext
    {
        public ActorContext(ActorWrapperBase self, IActorSystem actorSystem)
        {
            Self = self;
            System = actorSystem;
        }

        public  ActorWrapperBase Self { get; }
        public IActorSystem System { get; }
    }
}