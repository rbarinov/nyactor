using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;
using NYActor.Core;
using NYActor.EventStore;

namespace NYActor.Tests
{
    public class EventStoreLoadTests
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

            const int actorsCount = 1000;
            const int messagesPerActorCount = 100;

            var prefix = Guid.NewGuid().ToString();

            string Key(int i) => $"{prefix}-{i}";

            await Observable
                .Range(1, actorsCount)
                .SelectMany((i) => Observable
                    .Range(1, messagesPerActorCount)
                    .Select(n => Observable
                        .FromAsync(() => node.GetActor<EventStoreLoadActor>(Key(i))
                            .InvokeAsync(e => e.Apply($"Hello {n}"))
                        )
                    )
                )
                .Merge()
                .ToTask();


            await Observable
                .Range(1, actorsCount)
                .Select((i) => Observable
                    .FromAsync(async () =>
                        {
                            var messages = await node.GetActor<EventStoreLoadActor>(Key(i))
                                .InvokeAsync(e => e.GetInfo());

                            Assert.AreEqual(messagesPerActorCount, messages.Distinct().Count());
                            Assert.AreEqual(messagesPerActorCount, messages.Count());
                        }
                    ))
                .Merge()
                .ToTask();
        }

        public class EventStoreLoadActor : EventStorePersistedActor<EventStoreLoadState>
        {
            public EventStoreLoadActor(IEventStoreConnection eventStoreConnection)
                : base(eventStoreConnection)
            {
            }

            public async Task Apply(string message)
            {
                // await Task.Delay(5000);
                var ev = new EventStoreLoadApplyEvent(Key, DateTime.UtcNow, message);
                await ApplySingleAsync(ev);
                await Task.Delay(1000);
            }

            public Task<List<string>> GetInfo()
            {
                return Task.FromResult(State.Messages);
            }
        }

        public class EventStoreLoadApplyEvent
        {
            public string Key { get; }
            public DateTime EventAt { get; }
            public string Message { get; }

            public EventStoreLoadApplyEvent(string key, DateTime eventAt, string message)
            {
                Key = key;
                EventAt = eventAt;
                Message = message;
            }
        }

        public class EventStoreLoadState : IApplicable
        {
            public List<string> Messages { get; private set; } = new List<string>();

            public void Apply(object ev)
            {
                switch (ev)
                {
                    case EventStoreLoadApplyEvent applyEvent:
                        Messages.Add(applyEvent.Message);
                        break;
                }
            }
        }
    }
}