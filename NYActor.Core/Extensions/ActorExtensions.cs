using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NYActor.Core.RequestPropagation;

namespace NYActor.Core.Extensions
{
    public interface IExpressionCallable<TActor> where TActor : IActor
    {
        Task SendAsync<TMessage>(TMessage message);

        Task<TResult> InvokeAsync<TResult>(
            Expression<Func<TActor, Task<TResult>>> req,
            ActorExecutionContext executionContext = null
        );

        Task InvokeAsync(Expression<Func<TActor, Task>> req, ActorExecutionContext executionContext = null);

        IActorWrapper<TActor> Unwrap();
    }

    public class ExpressionCallable<TActor> : IExpressionCallable<TActor>
        where TActor : IActor
    {
        private readonly IActorWrapper<TActor> _actorWrapper;

        public ExpressionCallable(IActorWrapper<TActor> actorWrapper)
        {
            _actorWrapper = actorWrapper;
        }

        public Task SendAsync<TMessage>(TMessage message)
        {
            return _actorWrapper.SendAsync(message);
        }

        public Task<TResult> InvokeAsync<TResult>(
            Expression<Func<TActor, Task<TResult>>> req,
            ActorExecutionContext executionContext = null
        )
        {
            var callName = Regex.Match(req.Body.ToString(), @"([a-zA-Z0-9_]+)\(.+")
                .Groups[1]
                .Value;

            var func = req.Compile();

            return _actorWrapper.InvokeAsync(func, callName, executionContext);
        }

        public Task InvokeAsync(Expression<Func<TActor, Task>> req, ActorExecutionContext executionContext = null)
        {
            var callName = Regex.Match(req.Body.ToString(), @"([a-zA-Z0-9_]+)\(.+")
                .Groups[1]
                .Value;

            var func = req.Compile();

            return _actorWrapper.InvokeAsync(func, callName, executionContext);
        }

        public IActorWrapper<TActor> Unwrap()
        {
            return _actorWrapper;
        }
    }

    public static class ActorExtensions
    {
        public static IExpressionCallable<TActorBase> ToBaseRef<TActorBase>(
            this IActorWrapper<TActorBase> wrapper
        ) where TActorBase : IActor =>
            new ExpressionCallable<TActorBase>(wrapper);

        public static IActorWrapper<TActor> Self<TActor>(this TActor actor) where TActor : Actor =>
            actor.Context.Self.As<TActor>();

        public static IExpressionCallable<TActor> Wrap<TActor>(this IActorWrapper<TActor> wrapper)
            where TActor : Actor =>
            new ExpressionCallable<TActor>(wrapper);

        public static ActorExecutionContext ActorExecutionContext<TActor>(this TActor actor)
            where TActor : Actor
        {
            var genericActorWrapper = actor.Self() as GenericActorWrapper<TActor>;

            return genericActorWrapper?.ExecutionContext;
        }

        public static TContext To<TContext>(this ActorExecutionContext executionContext)
            where TContext : ActorExecutionContext =>
            executionContext as TContext;

        public static IActorSystem System<TActor>(this TActor actor, bool withEmptyExecutionContext = false)
            where TActor : Actor
        {
            if (withEmptyExecutionContext)
            {
                return actor.Context.System;
            }

            var requestPropagationExecutionContext = actor.ActorExecutionContext()
                ?.To<RequestPropagationExecutionContext>();

            if (requestPropagationExecutionContext == null)
                return actor.Context.System;

            return new RequestPropagationNodeWrapper(actor.Context.System, requestPropagationExecutionContext);
        }
    }
}
