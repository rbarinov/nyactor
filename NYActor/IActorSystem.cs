namespace NYActor;

public interface IActorSystem
{
    IActorReference<TActor> GetActor<TActor>(string key) where TActor : Actor;
}
