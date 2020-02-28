namespace NYActor.Core.Extensions
{
    public static class ActorExtensions
    {
        public static IActorWrapper<TActor> Self<TActor>(this TActor actor) where TActor : Actor =>
            actor.Context.Self.As<TActor>();

        public static IActorSystem System<TActor>(this TActor actor) where TActor : Actor => actor.Context.System;
    }
}