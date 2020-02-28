namespace NYActor.Core.Extensions
{
    public static class ActorWrapperExtensions
    {
        public static IActorWrapper<TActor> As<TActor>(this IActorWrapper actorWrapper) where TActor : Actor =>
            actorWrapper as IActorWrapper<TActor>;
    }
}