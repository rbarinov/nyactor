namespace NYActor.Core
{
    public interface IActorContext
    {
        public IActorWrapper Self { get; }
        public IActorSystem System { get; }
    }
}