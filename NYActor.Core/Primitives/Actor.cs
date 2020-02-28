using System.Threading.Tasks;

namespace NYActor.Core
{
    public class Actor
    {
        public string Key { get; internal set; }
        public IActorContext Context { get; set; }

        public async Task Activate()
        {
            await OnActivated().ConfigureAwait(false);
        }

        public virtual Task OnMessage(object message) => Task.CompletedTask;

        protected virtual Task OnActivated() => Task.CompletedTask;

        public async Task Deactivate()
        {
            await OnDeactivated().ConfigureAwait(false);
        }

        protected virtual Task OnDeactivated() => Task.CompletedTask;
    }
}