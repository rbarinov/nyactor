using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3
{
    public class DeactivationTest
    {
        const string Key = nameof(Key);

        private static int _activations;
        private static int _deactivations;

        [Test]
        public async Task Deactivate()
        {
            using var node = new ActorNodeBuilder()
                .WithActorDeactivationTimeout(TimeSpan.FromSeconds(1))
                .Build();

            var test = node.GetActor<DeactivationActor>(Key);

            _activations = 0;
            _deactivations = 0;

            Assert.AreEqual(0, _activations);
            Assert.AreEqual(0, _deactivations);

            foreach (var interval in Enumerable.Range(1, 5))
            {
                await Task.Delay(500);
                await test.InvokeAsync(e => e.Nope());
            }

            Assert.AreEqual(1, _activations);
            Assert.AreEqual(0, _deactivations);

            await Task.Delay(2000);

            Assert.AreEqual(1, _activations);
            Assert.AreEqual(1, _deactivations);

            await test.InvokeAsync(e => e.Nope());

            Assert.AreEqual(2, _activations);
            Assert.AreEqual(1, _deactivations);
        }

        [Test]
        public async Task LongDeactivation()
        {
            using var node = new ActorNodeBuilder()
                .WithActorDeactivationTimeout(TimeSpan.FromSeconds(1))
                .Build();

            var test = node.GetActor<LongDeactivationActor>(Key);

            _activations = 0;
            _deactivations = 0;

            Assert.AreEqual(0, _activations);
            Assert.AreEqual(0, _deactivations);

            await test.InvokeAsync(e => e.Nope());

            Assert.AreEqual(1, _activations);
            Assert.AreEqual(0, _deactivations);

            await Task.Delay(2000);

            Assert.AreEqual(1, _activations);
            Assert.AreEqual(0, _deactivations);
        }

        public class DeactivationActor : Actor
        {
            protected override Task OnActivated()
            {
                _activations++;

                return base.OnActivated();
            }

            protected override Task OnDeactivated()
            {
                _deactivations++;

                return base.OnDeactivated();
            }

            public Task Nope() =>
                Task.CompletedTask;
        }

        public class LongDeactivationActor : Actor
        {
            protected override async Task OnActivated()
            {
                _activations++;
                await base.OnActivated();

                DelayDeactivation(TimeSpan.FromHours(2));
            }

            protected override Task OnDeactivated()
            {
                _deactivations++;

                return base.OnDeactivated();
            }

            public Task Nope() =>
                Task.CompletedTask;
        }
    }
}
