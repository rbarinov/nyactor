using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;
using NYActor.Core;
using NYActor.EventStore;

namespace NYActor.Tests
{
    public class EventStoreHierarchyTests
    {
        private IEventStoreConnection _es;

        [SetUp]
        public async Task Setup()
        {
            var userCredentials = new UserCredentials("admin", "changeit");

            var connectionSettingsBuilder = ConnectionSettings.Create()
                .SetDefaultUserCredentials(userCredentials)
                .KeepReconnecting()
                .KeepRetrying()
                .Build();

            _es = EventStoreConnection.Create(
                connectionSettingsBuilder,
                new IPEndPoint(IPAddress.Any, 1113)
            );

            await _es.ConnectAsync();
        }

        [Test]
        public async Task Test()
        {
            var node = new Node()
                .ConfigureInjector(e => { e.RegisterInstance(_es); })
                .RegisterActorsFromAssembly(typeof(EventStoreTests).Assembly);

            var actor = node.GetActor<EventStoreHierarchyTests.B>();
            await actor.InvokeAsync(e => e.Foo());
        }

        public class A<T> : EventStorePersistedActor<T> where T : class, IApplicable, new()
        {
            public A(IEventStoreConnection eventStoreConnection)
                : base(eventStoreConnection)
            {
            }

            protected override async Task OnActivated()
            {
                await base.OnActivated();
                var a = 2 + 4;
            }
        }

        public class B : A<GenericState<BState>>
        {
            public B(IEventStoreConnection eventStoreConnection)
                : base(eventStoreConnection)
            {
            }

            protected override async Task OnActivated()
            {
                await base.OnActivated();
                var b = 2 + 4;
            }

            public Task Foo() =>
                Task.CompletedTask;
        }

        public class BState : IApplicable
        {
            public void Apply(object ev)
            {
            }
        }

        public class GenericState<T> : IApplicable
        {
            public T Payload { get; set; }

            public void Apply(object ev)
            {
            }
        }
    }
}
