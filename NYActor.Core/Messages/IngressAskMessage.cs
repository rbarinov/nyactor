using System;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public class IngressAskMessage : ActorMessage
    {
        public Func<Actor, Task<object>> Invoke { get; }
        public TaskCompletionSource<object> TaskCompletionSource { get; }
        public ActorExecutionContext ExecutionContext { get; }

        public IngressAskMessage(Func<Actor, Task<object>> invoke,
            TaskCompletionSource<object> taskCompletionSource,
            ActorExecutionContext executionContext)
        {
            Invoke = invoke;
            TaskCompletionSource = taskCompletionSource;
            ExecutionContext = executionContext;
        }
    }
}