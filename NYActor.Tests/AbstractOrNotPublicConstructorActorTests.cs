using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NYActor.Core;

namespace NYActor.Tests
{
    public class AbstractOrNotPublicConstructorActorTests
    {
        [Test]
        public async Task Test()
        {
            var node = new Node()
                .RegisterActorsFromAssembly(typeof(ProperActor).Assembly);

            var properActor = node.GetActor<ProperActor>();
            var innerProperActor = node.GetActor<ProperActor.InnerProperActor>();

            await properActor.InvokeAsync(e => e.Nope());
            await innerProperActor.InvokeAsync(e => e.Nope());
        }
    }

    public class ProperActor : Actor
    {
        public Task Nope() => Task.CompletedTask;

        public class InnerProperActor : Actor
        {
            public Task Nope() => Task.CompletedTask;
        }
    }

    public abstract class ExternalAbstractActor : Actor
    {
        public abstract class InternalAbstractActor : Actor
        {
            public Task Nope() => Task.CompletedTask;
        }

        public Task Nope() => Task.CompletedTask;
    }

    public class ExternalNoPublicConstructorActor : Actor
    {
        protected ExternalNoPublicConstructorActor()
        {
        }

        public Task Nope() => Task.CompletedTask;

        public class InternalNoPublicConstructorActor : Actor
        {
            public Task Nope() => Task.CompletedTask;

            protected InternalNoPublicConstructorActor()
            {
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
}