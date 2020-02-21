using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public class Actor
    {
        public string Key { get; internal set; }
        internal IActorContext Context { get; set; }

        internal async Task Activate()
        {
            await OnActivated().ConfigureAwait(false);
        }

        protected virtual Task OnActivated() => Task.CompletedTask;

        internal async Task Deactivate()
        {
            await OnDeactivated().ConfigureAwait(false);
        }

        protected virtual Task OnDeactivated() => Task.CompletedTask;
    }
}