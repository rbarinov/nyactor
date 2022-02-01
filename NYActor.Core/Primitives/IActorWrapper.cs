using System;
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
            string callName,
            ActorExecutionContext executionContext = null
        );

        Task InvokeAsync(Func<TActor, Task> req, string callName, ActorExecutionContext executionContext = null);
    }

    public class ActorExecutionContext
    {
        public static readonly ActorExecutionContext Empty = new ActorExecutionContext();
    }
}
