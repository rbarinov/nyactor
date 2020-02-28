using System;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public class IngressAskMessage : ActorMessage
    {
        public Func<Actor, Task<object>> Invoke { get; }
        public TaskCompletionSource<object> TaskCompletionSource { get; }

        public IngressAskMessage(Func<Actor, Task<object>> invoke,
            TaskCompletionSource<object> taskCompletionSource)
        {
            Invoke = invoke;
            TaskCompletionSource = taskCompletionSource;
        }
    }
}