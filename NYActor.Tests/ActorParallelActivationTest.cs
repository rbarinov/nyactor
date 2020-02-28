using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Core;

namespace NYActor.Tests
{
    public class ActorParallelActivationTest
    {
        public static readonly TimeSpan SlowActivationDelay = TimeSpan.FromSeconds(2);

        [Test]
        public async Task MessagePipe()
        {
            using var node = new Node();

            var slow = node.GetActor<SlowActivationActor>();
            var fast = node.GetActor<FastActivationActor>();

            var task = Task.Run(async () =>
            {
                var res = await slow.InvokeAsync(e => e.GetKey());
                return res;
            });

            await Task.Delay(1000);

            var sw = Stopwatch.StartNew();
            var fastRes = await fast.InvokeAsync(e => e.GetKey());
            sw.Stop();

            var elapsed = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds);

            Assert.True(elapsed < SlowActivationDelay.Subtract(TimeSpan.FromSeconds(1)) / 2);

            var slowRes = await task;
        }


        public class SlowActivationActor : Actor
        {
            protected override async Task OnActivated()
            {
                await base.OnActivated();
                await Task.Delay(ActorParallelActivationTest.SlowActivationDelay);
            }

            public Task<string> GetKey() => Task.FromResult(Key);
        }

        public class FastActivationActor : Actor
        {
            public Task<string> GetKey() => Task.FromResult(Key);
        }
    }
}