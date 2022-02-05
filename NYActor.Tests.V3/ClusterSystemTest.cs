using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Cluster;
using NYActor.OpenTelemetry;

namespace NYActor.Tests.V3;

public class ClusterSystemTest
{
    [Test]
    public async Task Test()
    {
        // using var cluster = new ActorSystemBuilder()
        using var cluster = new ClusterActorSystemBuilder()
            .AddOpenTelemetryTracing("192.168.49.2", 30031)
            .Build();

        using var scopedCluster = new ScopedActorSystem(
            cluster,
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"req", "new"}
                }
            )
        );

        // var direct = cluster.GetActor<DerivedWorkerActor>(nameof(cluster));
        // await direct.InvokeAsync(e => e.Foo());
        //
        // var scoped = scopedCluster.GetActor<DerivedWorkerActor>(nameof(scopedCluster));
        // await scoped.InvokeAsync(e => e.Foo());

        // var directLocal = cluster.GetActor<LocalTestActor>(nameof(cluster));
        // var res1 = await directLocal.InvokeAsync(e => e.Job());
        //
        var scopedLocal = scopedCluster.GetActor<LocalTestActor>(nameof(scopedCluster));
        var res2 = await scopedLocal.InvokeAsync(e => e.FirstLayerActorJob());

        res2 = await scopedLocal.InvokeAsync(e => e.FirstLayerActorJob());
        res2 = await scopedLocal.InvokeAsync(e => e.FirstLayerActorJob());
        res2 = await scopedLocal.InvokeAsync(e => e.FirstLayerActorJob());
        res2 = await scopedLocal.InvokeAsync(e => e.FirstLayerActorJob());
        //
        // var directBase = cluster.GetActor<WorkerActor>(nameof(cluster));
        // await directBase.InvokeAsync(e => e.Foo());
        //
        // var scopedBase = scopedCluster.GetActor<WorkerActor>(nameof(scopedCluster));
        // await scopedBase.InvokeAsync(e => e.Foo());
        await Task.Delay(5000);
    }
}

// [LocalActorNodeActor]
public class DerivedWorkerActor : WorkerActor
{
}

// [LocalActorNodeActor]
public class WorkerActor : Actor
{
    protected override Task OnActivated()
    {
        var self = this.Self();
        var sys = this.System();

        return base.OnActivated();
    }

    public async Task SecondLayerActorJob()
    {
        var context = this.ActorExecutionContext();
        var self = this.Self();
        var sys = this.System();

        var nestedCall = "call-to-new-nested-actor";

        if (Key != nestedCall)
        {
            var second = sys.GetActor<DerivedWorkerActor>(nestedCall);
            await second.InvokeAsync(e => e.SecondLayerActorJob());
        }
        // self.InvokeAsync(e => e.SelfInvokedFoo())
        //     .Ignore();
    }

    public Task SelfInvokedFoo()
    {
        var context = this.ActorExecutionContext();
        var self = this.Self();
        var sys = this.System();

        return Task.CompletedTask;
    }
}

[LocalActorNodeActor]
public class LocalTestActor : Actor
{
    public async Task<int> FirstLayerActorJob()
    {
        var context = this.ActorExecutionContext();
        var self = this.Self();
        var sys = this.System();

        await Task.Yield();

        await sys.GetActor<DerivedWorkerActor>(Key)
            .InvokeAsync(e => e.SecondLayerActorJob());

        return 1;
    }
}
