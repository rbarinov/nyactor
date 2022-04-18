using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NYActor.Patterns.Watchdog
{
    public abstract class WatchdogActor : Actor
    {
        private Subject<Unit> _unsubscribe;

        protected override async Task OnDeactivated()
        {
            _unsubscribe?.OnNext(Unit.Default);
            _unsubscribe?.OnCompleted();

            await base.OnDeactivated();
        }

        protected abstract IActorReference<IWatchdogClient> GetWatchableActor();

        public Task Watch()
        {
            _unsubscribe?.OnNext(Unit.Default);
            _unsubscribe?.OnCompleted();

            _unsubscribe = new Subject<Unit>();

            this.EnableDeactivationDelay(_unsubscribe);

            Observable.FromAsync(
                    async () =>
                    {
                        var context = await this.Self()
                            .InvokeAsync(e => e.GenerateExecutionContext(), ActorExecutionContext.Empty);

                        return await Observable
                            .FromAsync(
                                async () =>
                                {
                                    await GetWatchableActor()
                                        .InvokeAsync(w => w.Ping(), context);

                                    return context;
                                }
                            )
                            .Timeout(TimeSpan.FromSeconds(60))
                            .IgnoreElements()
                            .Catch<ActorExecutionContext, TimeoutException>(e => Observable.Return(context));
                    }
                )
                .RepeatAfterDelay(TimeSpan.FromSeconds(5))
                .Take(1)
                .Concat(
                    Observable.Empty<ActorExecutionContext>()
                        .Delay(TimeSpan.FromMinutes(15))
                )
                .Repeat()
                .Select(
                    e => Observable.FromAsync(
                        () => this.Self()
                            .InvokeAsync(
                                n => n.NotifyLocked(),
                                e
                            )
                    )
                )
                .Merge(1)
                .TakeUntil(_unsubscribe)
                .Subscribe();

            return Task.CompletedTask;
        }

        public Task<ActorExecutionContext> GenerateExecutionContext()
        {
            return Task.FromResult(this.ActorExecutionContext());
        }

        protected abstract Task NotifyLocked();

        public Task Unwatch()
        {
            _unsubscribe?.OnNext(Unit.Default);
            _unsubscribe?.OnCompleted();
            _unsubscribe = null;

            return Task.CompletedTask;
        }
    }
}
