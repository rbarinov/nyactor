using System;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Core;
using NYActor.Core.Extensions;

namespace NYActor.Tests
{
    public class SelfHierachyTests
    {
        [Test]
        public async Task TestHierarchyActorSelfCall()
        {
            var system = new Node()
                .RegisterActorsFromAssembly(typeof(A).Assembly);

            var key = nameof(TestHierarchyActorSelfCall);

            var actor = system.GetActor<B>(key);

            await actor.InvokeAsync(e => e.RunTest());
        }

        public class A : Actor
        {
            protected Task Test()
            {
                var actorWrapper = this.Self();
                actorWrapper.DelayDeactivation(TimeSpan.FromSeconds(1));
                return Task.CompletedTask;
            }

            public async Task Foo()
            {
                // some job
                await Task.Yield();
            }
        }

        public class B : A
        {
            public Task RunTest() => Test();
        }
    }
}