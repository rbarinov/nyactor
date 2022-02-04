namespace NYActor;

public interface IActorSystem : IDisposable
{
    IActorReference<TActor> GetActor<TActor>(string key) where TActor : Actor;
}
