using System;

namespace NYActor.Core
{
    public static class ActorExtensions
    {
        public static ActorWrapper<TActor> Self<TActor>(this TActor self) where TActor : Actor =>
            self.Context.Self.As<TActor>();

        public static IActorSystem System<TActor>(this TActor self) where TActor : Actor => self.Context.System;
    }
}