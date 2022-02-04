using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3;

public class SelfHierachyTests
{
    [Test]
    public async Task TestHierarchyActorSelfCall()
    {
        var system = new ActorSystemBuilder()
            .BuildLocalActorNode();

        var key = nameof(TestHierarchyActorSelfCall);

        var actor = system.GetActor<B>(key);

        await actor.InvokeAsync(e => e.RunTest());
    }

    public class A : Actor
    {
        protected async Task Test()
        {
            await Foo();

            var self = this.Self();

            if (self == null) throw new NotImplementedException();
        }

        public async Task Foo()
        {
            // some job
            await Task.Yield();
        }
    }

    public class B : A
    {
        public Task RunTest()
        {
            var self = this.Self();

            return Test();
        }
    }
}
