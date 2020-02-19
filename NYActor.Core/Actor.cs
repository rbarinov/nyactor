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
            await OnActivated();
        }

        protected virtual Task OnActivated() => Task.CompletedTask;

        internal async Task Deactivate()
        {
            await OnDeactivated();
        }

        protected virtual Task OnDeactivated() => Task.CompletedTask;
    }
}