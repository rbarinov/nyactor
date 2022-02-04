using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3;

public class ClusterSystemTest
{
    [Test]
    public async Task Test()
    {
        using var cluster = new ActorSystemBuilder()
            .BuildCluster();

        using var scopedCluster = new ScopedActorSystem(
            cluster,
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"req", "new"}
                }
            )
        );

        var direct = cluster.GetActor<DerivedWorkerActor>(nameof(cluster));
        await direct.InvokeAsync(e => e.Foo());

        var scoped = scopedCluster.GetActor<DerivedWorkerActor>(nameof(scopedCluster));
        await scoped.InvokeAsync(e => e.Foo());

        var directBase = cluster.GetActor<WorkerActor>(nameof(cluster));
        await directBase.InvokeAsync(e => e.Foo());

        var scopedBase = scopedCluster.GetActor<WorkerActor>(nameof(scopedCluster));
        await scopedBase.InvokeAsync(e => e.Foo());

        await Task.Delay(5000);
    }
}

public class DerivedWorkerActor : WorkerActor
{
}

public class WorkerActor : Actor
{
    protected override Task OnActivated()
    {
        var self = this.Self();
        var sys = this.System();

        return base.OnActivated();
    }

    public Task Foo()
    {
        var context = this.ActorExecutionContext();
        var self = this.Self();
        var sys = this.System();

        self.InvokeAsync(e => e.SelfInvokedFoo())
            .Ignore();

        return Task.CompletedTask;
    }

    public Task SelfInvokedFoo()
    {
        var context = this.ActorExecutionContext();
        var self = this.Self();
        var sys = this.System();

        return Task.CompletedTask;
    }
}
