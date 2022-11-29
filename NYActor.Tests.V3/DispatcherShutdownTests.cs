using System;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Message;

namespace NYActor.Tests.V3;

public class DispatcherShutdownTests
{
    [Test]
    public async Task DispatcherShutdown()
    {
        using var node = new ActorSystemBuilder()
            .WithActorDeactivationTimeout(TimeSpan.FromHours(1))
            .Build();

        var f = node.GetActor<ShutdownActor>("first");

        await f.SendAsync(new object());

        await Task.Delay(TimeSpan.FromSeconds(1));

        await f.SendAsync(PoisonPill.Default);

        await Task.Delay(TimeSpan.FromSeconds(1));

        await f.SendAsync(new object());

        await Task.Delay(TimeSpan.FromSeconds(1));

        await f.SendAsync(PoisonPill.Default);
    }
}

public class ShutdownActor : Actor
{
}
