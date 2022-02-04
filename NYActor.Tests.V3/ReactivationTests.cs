using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3
{
    public class ReactivationTests
    {
        [Test]
        public async Task Test()
        {
            using var node = new ActorNodeBuilder().Build();

            var actor = node.GetActor<ErrorActor>("a");

            await actor.InvokeAsync(e => e.Do());

            try
            {
                await actor.InvokeAsync(e => e.Error());
            }
            catch (Exception ex)
            {
                // ignore
            }

            await actor.InvokeAsync(e => e.Do());
        }

        public class ErrorActor : Actor
        {
            public Task Do()
            {
                return Task.CompletedTask;
            }

            public Task Error() =>
                throw new Exception();
        }
    }
}
