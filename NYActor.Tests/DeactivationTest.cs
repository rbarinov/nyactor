using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Core;
using NYActor.Core.Extensions;

namespace NYActor.Tests
{
    public class DeactivationTest
    {
        private static int activations;
        private static int deactivations;

        [Test]
        public async Task Deactivate()
        {
            using var node = new Node()
                .RegisterActorsFromAssembly(typeof(DeactivationTest).Assembly)
                .OverrideDefaultDeactivationTimeout(TimeSpan.FromSeconds(1));

            var test = node.GetActor<DeactivationActor>();

            activations = 0;
            deactivations = 0;

            Assert.AreEqual(0, activations);
            Assert.AreEqual(0, deactivations);

            foreach (var interval in Enumerable.Range(1, 5))
            {
                await Task.Delay(500);
                await test.InvokeAsync(e => e.Nope());
            }

            Assert.AreEqual(1, activations);
            Assert.AreEqual(0, deactivations);

            await Task.Delay(2000);

            Assert.AreEqual(1, activations);
            Assert.AreEqual(1, deactivations);

            await test.InvokeAsync(e => e.Nope());

            Assert.AreEqual(2, activations);
            Assert.AreEqual(1, deactivations);
        }

        [Test]
        public async Task LongDeactivation()
        {
            var node = new Node()
                .RegisterActorsFromAssembly(typeof(DeactivationTest).Assembly)
                .OverrideDefaultDeactivationTimeout(TimeSpan.FromSeconds(1));

            var test = node.GetActor<LongDeactivationActor>();

            activations = 0;
            deactivations = 0;

            Assert.AreEqual(0, activations);
            Assert.AreEqual(0, deactivations);

            await test.InvokeAsync(e => e.Nope());

            Assert.AreEqual(1, activations);
            Assert.AreEqual(0, deactivations);

            await Task.Delay(2000);

            Assert.AreEqual(1, activations);
            Assert.AreEqual(0, deactivations);
        }

        public class DeactivationActor : Actor
        {
            protected override Task OnActivated()
            {
                activations++;
                return base.OnActivated();
            }

            protected override Task OnDeactivated()
            {
                deactivations++;
                return base.OnDeactivated();
            }

            public Task Nope() => Task.CompletedTask;
        }

        public class LongDeactivationActor : Actor
        {
            protected override async Task OnActivated()
            {
                activations++;
                await base.OnActivated();

                this.Self().DelayDeactivation(TimeSpan.FromHours(2));
            }

            protected override Task OnDeactivated()
            {
                deactivations++;
                return base.OnDeactivated();
            }

            public Task Nope() => Task.CompletedTask;
        }
    }
}