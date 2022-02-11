using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NYActor.Cluster;
using NYActor.Cluster.ClusterNodeDiscovery;
using NYActor.Cluster.ClusterNodeDiscovery.Discovery;

namespace NYActor.Tests.V3;

public class ClusterNodeDiscoveryTests
{
    [Test]
    public async Task StaticClusterNodeDiscovery()
    {
        IClusterNodeDiscoveryProvider provider = new StaticClusterNodeDiscoveryProvider(
            TimeSpan.FromMinutes(5),
            new ClusterNodeDiscoveryNodeInfo(
                "127.0.0.1",
                1338
            ),
            new ClusterNodeDiscoveryNodeInfo(
                "127.0.0.1",
                1339
            )
        );

        var nodes = await provider.DiscoverAsync();

        Assert.IsNotNull(nodes);
        Assert.AreEqual(2, nodes.Count);

        Assert.AreEqual(
            "127.0.0.1",
            nodes.ElementAt(0)
                .Address
        );

        Assert.AreEqual(
            1338,
            nodes.ElementAt(0)
                .Port
        );

        Assert.AreEqual(
            "127.0.0.1",
            nodes.ElementAt(1)
                .Address
        );

        Assert.AreEqual(
            1339,
            nodes.ElementAt(1)
                .Port
        );
    }

    [Test]
    public async Task StaticClusterNodeDiscoveryActor()
    {
        IClusterNodeDiscoveryProvider provider = new StaticClusterNodeDiscoveryProvider(
            TimeSpan.FromSeconds(1),
            new ClusterNodeDiscoveryNodeInfo(
                "127.0.0.1",
                1338
            ),
            new ClusterNodeDiscoveryNodeInfo(
                "127.0.0.1",
                1339
            )
        );

        var node = new ClusterActorSystemBuilder()
            .ConfigureServices(
                e =>
                {
                    e.AddSingleton(provider);
                    e.AddSingleton<ITimeProvider>(new NaturalTimeProvider());
                }
            )
            .Build();

        var info = await Observable.FromAsync(
                () => node.GetActor<ClusterNodeDiscoveryActor>(Actor.Single)
                    .InvokeAsync(e => e.GetInfo())
            )
            .RepeatAfterDelay(TimeSpan.FromSeconds(1))
            .FirstAsync(e => e != null);

        int i = 0;

        do
        {
            await Task.Delay(200);

            info = await node.GetActor<ClusterNodeDiscoveryActor>(Actor.Single)
                .InvokeAsync(e => e.GetInfo());

            i++;
        } while (i < 10);
    }

    [Test]
    public async Task DnsClusterNodeDiscovery()
    {
        IClusterNodeDiscoveryProvider provider = new DnsClusterNodeDiscoveryProvider(
            TimeSpan.FromMinutes(5),
            "yandex.ru",
            443
        );

        var nodes = await provider.DiscoverAsync();
    }

    [Test]
    public async Task DnsClusterNodeDiscoveryActor()
    {
        IClusterNodeDiscoveryProvider clusterDiscoveryProvider = new DnsClusterNodeDiscoveryProvider(
            TimeSpan.FromSeconds(1),
            "yandex.ru",
            443
        );

        var node = new ClusterActorSystemBuilder()
            .ConfigureServices(
                e =>
                {
                    e.AddSingleton(clusterDiscoveryProvider);
                    e.AddSingleton<ITimeProvider>(new NaturalTimeProvider());
                }
            )
            .Build();

        var info = await Observable.FromAsync(
                () => node.GetActor<ClusterNodeDiscoveryActor>(Actor.Single)
                    .InvokeAsync(e => e.GetInfo())
            )
            .RepeatAfterDelay(TimeSpan.FromSeconds(1))
            .FirstAsync(e => e != null);

        int i = 0;

        do
        {
            await Task.Delay(200);

            info = await node.GetActor<ClusterNodeDiscoveryActor>(Actor.Single)
                .InvokeAsync(e => e.GetInfo());

            i++;
        } while (i < 10);
    }
}
