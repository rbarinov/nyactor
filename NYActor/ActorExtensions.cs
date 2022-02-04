namespace NYActor;

public static class ActorExtensions
{
    public static IActorReference<TActor> Self<TActor>(this TActor actor) where TActor : Actor
    {
        var typedActorReferenceInternal = actor.SelfDispatcherInternal as IActorDispatcherInternal<TActor>;
        var actorReference = new LocalActorReference<TActor>(typedActorReferenceInternal);

        return actorReference;
    }

    public static IActorSystem System<TActor>(this TActor actor, ActorExecutionContext actorExecutionContext = null)
        where TActor : Actor
    {
        if (actorExecutionContext == NYActor.ActorExecutionContext.Empty)
        {
            return actor.SelfDispatcherInternal.ActorNode;
        }

        if (actor.ActorExecutionContext() is ScopedExecutionContext scopedExecutionContext)
        {
            return new ScopedActorSystem(actor.SelfDispatcherInternal.ActorNode, scopedExecutionContext);
        }

        return actor.SelfDispatcherInternal.ActorNode;
    }

    public static ActorExecutionContext ActorExecutionContext<TActor>(this TActor actor)
        where TActor : Actor
    {
        return (actor.SelfDispatcherInternal as ActorDispatcher<TActor>)?.CurrentExecutionContext;
    }

    public static TContext To<TContext>(this ActorExecutionContext executionContext)
        where TContext : ActorExecutionContext
    {
        return executionContext as TContext;
    }
}
