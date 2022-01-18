using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public interface IActorWrapper
    {
        void DelayDeactivation(TimeSpan deactivationTimeout);
    }

    public interface IActorWrapper<out TActor> : IActorWrapper where TActor : IActor
    {
        Task SendAsync<TMessage>(TMessage message);

        Task<TResult> InvokeAsync<TResult>(
            Func<TActor, Task<TResult>> req,
            ActorExecutionContext executionContext = null
        );

        Task InvokeAsync(Func<TActor, Task> req, ActorExecutionContext executionContext = null);
    }

    public class ActorExecutionContext
    {
    }
}
