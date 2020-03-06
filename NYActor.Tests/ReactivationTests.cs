using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Core;

namespace NYActor.Tests
{
    public class ReactivationTests
    {
        [Test]
        public async Task Test()
        {
            using var node = new Node()
                .RegisterActorsFromAssembly(typeof(ErrorActor).Assembly);

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

            public Task Error() => throw new Exception();
        }
    }
}