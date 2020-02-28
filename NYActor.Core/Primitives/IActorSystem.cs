namespace NYActor.Core
{
    public interface IActorSystem
    {
        IActorWrapper<TActor> GetActor<TActor>(string key) where TActor : Actor;
        IActorWrapper<TActor> GetActor<TActor>() where TActor : Actor;
    }
}