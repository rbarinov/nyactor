namespace NYActor;

public class ScopedActorSystem : IActorSystem
{
    private readonly IActorSystem _actorSystemInstance;
    private readonly ScopedExecutionContext _scopedExecutionContext;

    public ScopedActorSystem(
        IActorSystem actorSystemInstance,
        ScopedExecutionContext scopedExecutionContext
    )
    {
        _actorSystemInstance = actorSystemInstance;
        _scopedExecutionContext = scopedExecutionContext;
    }

    public IActorReference<TActor> GetActor<TActor>(string key) where TActor : Actor
    {
        var actorReference = _actorSystemInstance.GetActor<TActor>(key);

        return new ScopedActorReference<TActor>(actorReference, _scopedExecutionContext);
    }

    public void Dispose()
    {
        // ignore
    }
}
