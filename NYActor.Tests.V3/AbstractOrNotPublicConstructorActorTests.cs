using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3;

public class AbstractOrNotPublicConstructorActorTests
{
    private const string Key = nameof(Key);

    [Test]
    public async Task Test()
    {
        var node = new ActorNodeBuilder()
            .Build();

        var properActor = node.GetActor<ProperActor>(Key);
        var innerProperActor = node.GetActor<ProperActor.InnerProperActor>(Key);

        await properActor.InvokeAsync(e => e.Nope());
        await innerProperActor.InvokeAsync(e => e.Nope());
    }
}

public class ProperActor : Actor
{
    public Task Nope()
    {
        return Task.CompletedTask;
    }

    public class InnerProperActor : Actor
    {
        public Task Nope()
        {
            return Task.CompletedTask;
        }
    }
}

public abstract class ExternalAbstractActor : Actor
{
    public Task Nope()
    {
        return Task.CompletedTask;
    }

    public abstract class InternalAbstractActor : Actor
    {
        public Task Nope()
        {
            return Task.CompletedTask;
        }
    }
}

public class ExternalNoPublicConstructorActor : Actor
{
    protected ExternalNoPublicConstructorActor()
    {
    }

    public Task Nope()
    {
        return Task.CompletedTask;
    }

    public class InternalNoPublicConstructorActor : Actor
    {
        protected InternalNoPublicConstructorActor()
        {
        }

        public Task Nope()
        {
            return Task.CompletedTask;
        }
    }
}

public class ExternalNoPublicConstructorActor2 : Actor
{
    private ExternalNoPublicConstructorActor2()
    {
    }

    public class InternalNoPublicConstructorActor2 : Actor
    {
        private InternalNoPublicConstructorActor2()
        {
        }
    }
}
