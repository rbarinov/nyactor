using System;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public interface IActorWrapper
    {
        void DelayDeactivation(TimeSpan deactivationTimeout);
    }

    public interface IActorWrapper<out TActor> : IActorWrapper where TActor : Actor
    {
        Task SendAsync<TMessage>(TMessage message);
        Task<TResult> InvokeAsync<TResult>(Func<TActor, Task<TResult>> req);
        Task InvokeAsync(Func<TActor, Task> req);
    }
}