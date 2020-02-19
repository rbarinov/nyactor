using System;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public class MessageQueueItem
    {
        public Func<Actor, Task<object>> Req { get; set; }
        public TaskCompletionSource<object> Tsc { get; set; }
    }
}