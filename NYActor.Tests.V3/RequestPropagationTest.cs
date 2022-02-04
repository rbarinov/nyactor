using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace NYActor.Tests.V3;

public interface IClient
{
    Task<string> A(Dictionary<string, string> headers);
}

public class Client : IClient
{
    public Task<string> A(Dictionary<string, string> headers)
    {
        // @todo inject headers
        return Task.FromResult(headers?.GetValueOrDefault("key") ?? "default");
    }
}

public class MyActor : Actor
{
    private readonly IClient _client;

    public MyActor(IClient client)
    {
        _client = client;
    }

    public static int activationId = 0;

    protected override Task OnActivated()
    {
        activationId++;

        return base.OnActivated();
    }

    public async Task<string> Job()
    {
        var scope = this.ActorExecutionContext()
            ?.To<ScopedExecutionContext>()
            ?.Scope;

        return activationId + " " + await _client.A(scope);
    }
}

public class FacadeActor : Actor
{
    public async Task<string> FacadeJob()
    {
        var chain = this.System()
            .GetActor<MyActor>(Key);

        return await chain.InvokeAsync(e => e.Job());
    }
}

public class DoubleFacadeActor : Actor
{
    public async Task<string> DoubleFacadeJob()
    {
        var chain = this.System()
            .GetActor<FacadeActor>(Key);

        return await chain.InvokeAsync(e => e.FacadeJob());
    }
}

public class RequestPropagationTest
{
    [Test]
    public async Task TestDirect()
    {
        MyActor.activationId = 0;
        
        var client = new Client();

        var node = new ActorNodeBuilder()
            .ConfigureServices(
                e => { e.AddSingleton<IClient>(new Client()); }
            )
            .Build();

        string key = nameof(key);

        IActorReference<MyActor> actor;

        // no req

        var executionContext =
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"key", "noreq-context"}
                }
            );

        var noReqActorSystem = new ScopedActorSystem(node, executionContext);
        actor = noReqActorSystem.GetActor<MyActor>(key);
        var res = await actor.InvokeAsync(e => e.Job());
        Assert.AreEqual("1 noreq-context", res);

        // req x-1

        executionContext =
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"key", "req1-context"}
                }
            );

        var req1Context = new ScopedActorSystem(node, executionContext);
        actor = req1Context.GetActor<MyActor>(key);
        res = await actor.InvokeAsync(e => e.Job());
        Assert.AreEqual("1 req1-context", res);

        // req x-2

        executionContext =
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"key", "req2-context"}
                }
            );

        var req2Context = new ScopedActorSystem(node, executionContext);

        actor = req2Context.GetActor<MyActor>(key);
        res = await actor.InvokeAsync(e => e.Job());
        Assert.AreEqual("1 req2-context", res);
    }

    [Test]
    public async Task TestFacade()
    {
        MyActor.activationId = 0;
        
        var client = new Client();

        var node = new ActorNodeBuilder()
            .ConfigureServices(
                e => { e.AddSingleton<IClient>(new Client()); }
            )
            .Build();

        string key = nameof(key);

        IActorReference<DoubleFacadeActor> actor;

        // no req

        var executionContext =
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"key", "noreq-context"}
                }
            );

        var noReqActorSystem = new ScopedActorSystem(node, executionContext);
        actor = noReqActorSystem.GetActor<DoubleFacadeActor>(key);
        var res = await actor.InvokeAsync(e => e.DoubleFacadeJob());
        Assert.AreEqual("1 noreq-context", res);

        // req x-1

        executionContext =
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"key", "req1-context"}
                }
            );

        var req1Context = new ScopedActorSystem(node, executionContext);
        actor = req1Context.GetActor<DoubleFacadeActor>(key);
        res = await actor.InvokeAsync(e => e.DoubleFacadeJob());
        Assert.AreEqual("1 req1-context", res);

        // req x-2

        executionContext =
            new ScopedExecutionContext(
                new Dictionary<string, string>
                {
                    {"key", "req2-context"}
                }
            );

        var req2Context = new ScopedActorSystem(node, executionContext);

        actor = req2Context.GetActor<DoubleFacadeActor>(key);
        res = await actor.InvokeAsync(e => e.DoubleFacadeJob());
        Assert.AreEqual("1 req2-context", res);
    }
}
