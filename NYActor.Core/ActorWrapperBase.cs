using System;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public abstract class ActorWrapperBase : IDisposable
    {
        protected readonly string ActorPath;

        protected ActorWrapperBase(string actorPath)
        {
            ActorPath = actorPath;
        }

        internal abstract Task OnMessageEnqueued(MessageQueueItem messageQueueItem);

        public ActorWrapper<TActor> As<TActor>() where TActor : Actor => this as ActorWrapper<TActor>;

        public virtual void Dispose()
        {
        }
    }
}