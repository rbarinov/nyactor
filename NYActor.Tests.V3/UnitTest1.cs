using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace NYActor.Tests.V3;

public class Tests
{
    public class MyActor : Actor
    {
        private readonly string _testInjection;

        public MyActor(string testInjection)
        {
            _testInjection = testInjection;
        }

        public async Task<int> JobA()
        {
            this.Self()
                .InvokeAsync(e => e.JobBackground(), ActorExecutionContext.Empty)
                .Ignore();

            await Task.Yield();

            return 13;
        }

        public async Task<string> GetString()
        {
            await Task.Yield();

            return _testInjection;
        }

        public Task JobBackground()
        {
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Actor_inside_api_is_ok()
    {
        var node = new ActorNodeBuilder()
            .ConfigureServices(
                e => { e.AddSingleton<string>("injected-string"); }
            )
            .Build();

        var actorRef = node.GetActor<MyActor>("test");

        var res = await actorRef.InvokeAsync(e => e.JobA());

        Assert.AreEqual(13, res);

        var testInjection = await actorRef.InvokeAsync(e => e.GetString());

        Assert.AreEqual("injected-string", testInjection);
    }
}
