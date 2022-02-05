namespace NYActor;

public static class ActorExtensions
{
    public static IActorReference<TActor> Self<TActor>(this TActor actor) where TActor : Actor
    {
        var dispatcher = actor.SelfDispatcher as ILocalActorDispatcher<TActor>;

        var createScopedReference = dispatcher?.CurrentExecutionContext is ScopedExecutionContext;

        IActorReference<TActor> actorReference = new LocalActorReference<TActor>(dispatcher);

        if (createScopedReference)
        {
            actorReference = new ScopedActorReference<TActor>(
                actorReference,
                dispatcher?.CurrentExecutionContext.To<ScopedExecutionContext>()
            );
        }

        return actorReference;
    }

    public static IActorSystem System<TActor>(this TActor actor, ActorExecutionContext actorExecutionContext = null)
        where TActor : Actor
    {
        if (actorExecutionContext == NYActor.ActorExecutionContext.Empty)
            return actor.SelfDispatcher.ActorSystem;

        if (actor.ActorExecutionContext() is ScopedExecutionContext scopedExecutionContext)
            return new ScopedActorSystem(actor.SelfDispatcher.ActorSystem, scopedExecutionContext);

        return actor.SelfDispatcher.ActorSystem;
    }

    public static ActorExecutionContext ActorExecutionContext<TActor>(this TActor actor)
        where TActor : Actor
    {
        return (actor.SelfDispatcher as ILocalActorDispatcher<TActor>)?.CurrentExecutionContext;
    }

    public static TContext To<TContext>(this ActorExecutionContext executionContext)
        where TContext : ActorExecutionContext
    {
        return executionContext as TContext;
    }
}
