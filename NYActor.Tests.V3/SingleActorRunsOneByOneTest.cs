using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3;

public class SingleActorRunsOneByOneTest
{
    private const string Key = nameof(Key);

    private const int FiveSecDelay = 5000;

    [Test]
    public async Task TestOneByOne()
    {
        using var node = new ActorSystemBuilder().Build();

        var actor = node.GetActor<SingleActor>(Key);

        var task = Task.Run(() => actor.InvokeAsync(e => e.DelayLong()));

        var sw = Stopwatch.StartNew();

        var waitTaskToStart = 500;
        await Task.Delay(waitTaskToStart);

        var secondDelay = await actor.InvokeAsync(e => e.DelayFast());

        sw.Stop();
        var elapsed = sw.ElapsedMilliseconds;

        Assert.True(elapsed > FiveSecDelay - waitTaskToStart);

        var firstDelay = await task;
    }

    public class SingleActor : Actor
    {
        public async Task<int> DelayLong()
        {
            await Task.Delay(FiveSecDelay);

            return FiveSecDelay;
        }

        public async Task<int> DelayFast()
        {
            const int millisecondsDelay = 20;
            await Task.Delay(millisecondsDelay);

            return millisecondsDelay;
        }
    }
}
