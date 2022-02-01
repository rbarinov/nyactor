using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Core;
using NYActor.Core.Extensions;

namespace NYActor.Tests
{
    public class HierarchyWatchTests
    {
        public static readonly TaskCompletionSource<Unit> tsc = new TaskCompletionSource<Unit>();

        [Test]
        public async Task Test()
        {
            var node = new Node().RegisterActorsFromAssembly(typeof(HierarchyWatchTests).Assembly);

            var a = node.GetActor<WatchdogClientActorA>("KEY");

            await a.InvokeAsync(e => e.Foo());

            await Task.Delay(1000);

            await a.InvokeAsync(e => e.Foo());

            await Task.Delay(1000);

            var deadlock = a.InvokeAsync(e => e.Wait());

            await Task.Delay(20000);

            tsc.SetResult(Unit.Default);

            await deadlock;

            await Task.Delay(1000);

            a.InvokeAsync(e => e.Wait());
            await Task.Delay(10000);
        }

        public class WatchdogClientActorA : WatchdogClientActor, IWatchdogClient
        {
            protected override IExpressionCallable<WatchdogActor> GetWatchDog() =>
                this.System()
                    .GetActor<WatchdogActorA>(Key)
                    .Unwrap()
                    .ToBaseRef<WatchdogActor>();

            public Task Foo() =>
                Task.CompletedTask;

            public async Task Wait()
            {
                await tsc.Task;
            }
        }

        public class WatchdogClientActorB : WatchdogClientActor, IWatchdogClient
        {
            protected override IExpressionCallable<WatchdogActor> GetWatchDog() =>
                this.System()
                    .GetActor<WatchdogActorB>(Key)
                    .Unwrap()
                    .ToBaseRef<WatchdogActor>();
        }

        public abstract class WatchdogClientActor : Actor
        {
            protected abstract IExpressionCallable<WatchdogActor> GetWatchDog();

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
            protected abstract IExpressionCallable<IWatchdogClient> GetWatchdogClient();

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
            protected override IExpressionCallable<IWatchdogClient> GetWatchdogClient() =>
                this.System()
                    .GetActor<WatchdogClientActorA>(Key)
                    .Unwrap()
                    .ToBaseRef<IWatchdogClient>();
        }

        public class WatchdogActorB : WatchdogActor
        {
            protected override IExpressionCallable<IWatchdogClient> GetWatchdogClient() =>
                this.System()
                    .GetActor<WatchdogClientActorB>(Key)
                    .Unwrap()
                    .ToBaseRef<IWatchdogClient>();
        }
    }
}
