using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3
{
    public class HierarchyWatchTests
    {
        public static readonly TaskCompletionSource<Unit> Tsc = new();

        [Test]
        public async Task Test()
        {
            var node = new ActorSystemBuilder().Build();

            var a = node.GetActor<WatchdogClientActorA>("KEY");

            await a.InvokeAsync(e => e.Foo());

            await Task.Delay(1000);

            await a.InvokeAsync(e => e.Foo());

            await Task.Delay(1000);

            var deadlock = a.InvokeAsync(e => e.Wait());

            await Task.Delay(20000);

            Tsc.SetResult(Unit.Default);

            await deadlock;

            await Task.Delay(1000);

            a.InvokeAsync(e => e.Wait())
                .Ignore();

            await Task.Delay(10000);
        }

        public class WatchdogClientActorA : WatchdogClientActor, IWatchdogClient
        {
            protected override IActorReference<WatchdogActor> GetWatchDog() =>
                this.System()
                    .GetActor<WatchdogActorA>(Key)
                    .ToBaseRef<WatchdogActor>();

            public Task Foo() =>
                Task.CompletedTask;

            public async Task Wait()
            {
                await Tsc.Task;
            }
        }

        public class WatchdogClientActorB : WatchdogClientActor, IWatchdogClient
        {
            protected override IActorReference<WatchdogActor> GetWatchDog() =>
                this.System()
                    .GetActor<WatchdogActorB>(Key)
                    .ToBaseRef<WatchdogActor>();
        }

        public abstract class WatchdogClientActor : Actor
        {
            protected abstract IActorReference<WatchdogActor> GetWatchDog();

            protected override async Task OnActivated()
            {
                await base.OnActivated();

                await GetWatchDog()
                    .InvokeAsync(e => e.Watch());
            }

            protected override async Task OnDeactivated()
            {
                await GetWatchDog()
                    .InvokeAsync(e => e.Unwatch());

                await base.OnDeactivated();
            }
        }

        public interface IWatchdogClient : IActor
        {
            public Task WatchdogPing() =>
                Task.CompletedTask;
        }

        public abstract class WatchdogActor : Actor
        {
            protected abstract IActorReference<IWatchdogClient> GetWatchdogClient();

            private ISubject<Unit> _unsubscribe;

            public Task Watch()
            {
                _unsubscribe?.OnCompleted();
                _unsubscribe = new Subject<Unit>();

                Observable.Interval(TimeSpan.FromSeconds(1))
                    .TakeUntil(_unsubscribe)
                    .Do(_ => Console.WriteLine($"test: {Key}"))
                    .Select(
                        e => Observable.FromAsync(
                                () => GetWatchdogClient()
                                    .InvokeAsync(e => e.WatchdogPing())
                            )
                            .Timeout(TimeSpan.FromSeconds(2))
                            .IgnoreElements()
                            .Catch<Unit, TimeoutException>(e => Observable.Return(Unit.Default))
                    )
                    .Merge(1)
                    .Do(_ => Console.WriteLine($"had timeout: {Key}"))
                    .Subscribe();

                return Task.CompletedTask;
            }

            public Task Unwatch()
            {
                _unsubscribe?.OnCompleted();

                return Task.CompletedTask;
            }
        }

        public class WatchdogActorA : WatchdogActor
        {
            protected override IActorReference<IWatchdogClient> GetWatchdogClient() =>
                this.System()
                    .GetActor<WatchdogClientActorA>(Key)
                    .ToBaseRef<IWatchdogClient>();
        }

        public class WatchdogActorB : WatchdogActor
        {
            protected override IActorReference<IWatchdogClient> GetWatchdogClient() =>
                this.System()
                    .GetActor<WatchdogClientActorB>(Key)
                    .ToBaseRef<IWatchdogClient>();
        }
    }
}
