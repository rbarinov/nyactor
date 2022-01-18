using System.Runtime.CompilerServices;
using NYActor.Core.RequestPropagation;

namespace NYActor.Core.Extensions
{
    public static class ActorExtensions
    {
        public static IActorWrapper<TActor> Self<TActor>(this TActor actor) where TActor : Actor =>
            actor.Context.Self.As<TActor>();

        public static ActorExecutionContext ActorExecutionContext<TActor>(this TActor actor)
            where TActor : Actor
        {
            var genericActorWrapper = actor.Self() as GenericActorWrapper<TActor>;

            return genericActorWrapper?.ExecutionContext;
        }

        public static TContext To<TContext>(this ActorExecutionContext executionContext)
            where TContext : ActorExecutionContext =>
            executionContext as TContext;

        public static IActorSystem System<TActor>(this TActor actor) where TActor : Actor
        {
            var requestPropagationExecutionContext = actor.ActorExecutionContext()
                ?.To<RequestPropagationExecutionContext>();

            if (requestPropagationExecutionContext == null)
                return actor.Context.System;

            return new RequestPropagationNodeWrapper(actor.Context.System, requestPropagationExecutionContext);
        }
    }
}
