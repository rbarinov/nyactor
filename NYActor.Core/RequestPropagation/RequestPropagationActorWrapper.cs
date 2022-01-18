using System;
using System.Threading.Tasks;

namespace NYActor.Core.RequestPropagation
{
    public class RequestPropagationActorWrapper<TActor> : IActorWrapper<TActor> where TActor : Actor
    {
        public IActorWrapper<TActor> Actor { get; }
        public RequestPropagationExecutionContext RequestPropagationExecutionContext { get; }

        public RequestPropagationActorWrapper(
            IActorWrapper<TActor> actor,
            RequestPropagationExecutionContext requestPropagationExecutionContext
        )
        {
            Actor = actor;
            RequestPropagationExecutionContext = requestPropagationExecutionContext;
        }

        public void DelayDeactivation(TimeSpan deactivationTimeout)
        {
            Actor.DelayDeactivation(deactivationTimeout);
        }

        public Task SendAsync<TMessage>(TMessage message)
        {
            return Actor.SendAsync(message);
        }

        public Task<TResult> InvokeAsync<TResult>(
            Func<TActor, Task<TResult>> req,
            ActorExecutionContext executionContext
        )
        {
            return Actor.InvokeAsync(req, RequestPropagationExecutionContext);
        }

        public Task InvokeAsync(Func<TActor, Task> req, ActorExecutionContext executionContext)
        {
            return Actor.InvokeAsync(req, RequestPropagationExecutionContext);
        }
    }
}