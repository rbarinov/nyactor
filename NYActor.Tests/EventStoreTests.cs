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
    public class EventStoreTests
    {
        [Test]
        public async Task Test()
        {
            var userCredentials = new UserCredentials("admin", "changeit");
            var connectionSettingsBuilder = ConnectionSettings.Create()
                .SetDefaultUserCredentials(userCredentials)
                .Build();

            var es = EventStoreConnection.Create(connectionSettingsBuilder,
                new IPEndPoint(IPAddress.Any, 1113));

            await es.ConnectAsync();

            var node = new Node()
                .ConfigureInjector(e => { e.RegisterInstance(es); })
                .RegisterActorsFromAssembly(typeof(EventStoreTests).Assembly);

            var singleEventsRead = await node.GetActor<TestActor>("single").InvokeAsync(e => e.GetEventsRead());
            var multipleEventsRead = await node.GetActor<TestActor>("multiple").InvokeAsync(e => e.GetEventsRead());
            var multiple2EventsRead = await node.GetActor<TestActor>("multiple2").InvokeAsync(e => e.GetEventsRead());

            await node.GetActor<TestActor>("single").InvokeAsync(e => e.TestSingle());
            await node.GetActor<TestActor>("multiple").InvokeAsync(e => e.TestMultiple(10));
            await node.GetActor<TestActor>("multiple2").InvokeAsync(e => e.TestMultiple(10000));
        }

        public class State : IApplicable
        {
            public int EventsRead { get; set; } = 0;

            public void Apply(object ev)
            {
                if (ev is Event)
                {
                    EventsRead++;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public class Event
        {
            public int Data { get; }

            public Event(int data)
            {
                Data = data;
            }
        }

        public class TestActor : EventStorePersistedActor<State>
        {
            public TestActor(IEventStoreConnection eventStoreConnection) : base(eventStoreConnection)
            {
            }

            public Task<int> GetEventsRead() => Task.FromResult(State.EventsRead);

            public async Task TestSingle()
            {
                var @event = new Event(-1);
                await ApplySingleAsync(@event);
            }

            public async Task TestMultiple(int num)
            {
                var events = Enumerable.Range(0, num).Select(e => new Event(e));
                await ApplyMultipleAsync(events);
            }
        }
    }
}