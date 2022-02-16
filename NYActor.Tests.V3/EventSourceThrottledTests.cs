using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NYActor.EventSourcing;
using NYActor.EventSourcing.EventStore.v5;
using NYActor.Patterns.Throttled;

namespace NYActor.Tests.V3;

public class EventSourceThrottledTests
{
    private IActorSystem _actorSystem;

    [SetUp]
    public async Task SetUp()
    {
        var connectionSettingsBuilder = ConnectionSettings.Create()
            .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"))
            .KeepReconnecting()
            .KeepRetrying()
            .Build();

        var eventStoreAddress = Dns.GetHostAddresses("127.0.0.1")
            .First()
            .MapToIPv4();

        var eventStoreConnection = EventStoreConnection.Create(
            connectionSettingsBuilder,
            new IPEndPoint(eventStoreAddress, 1113)
        );

        await eventStoreConnection.ConnectAsync();

        _actorSystem = new ActorSystemBuilder()
            .ConfigureServices(
                e =>
                {
                    e.AddSingleton<ITimeProvider>(new NaturalTimeProvider());

                    e.AddSingleton<IEventSourcePersistenceProvider>(
                        new EventStoreV5EventSourcePersistenceProvider(eventStoreConnection, 500)
                    );
                }
            )
            .Build();
    }

    [Test]
    public async Task Test()
    {
        var key = Guid.NewGuid()
            .ToString();

        var throttled = _actorSystem.GetActor<SimpleActor>(key);

        await Observable
            .Range(0, 15)
            .Select(
                e => Observable
                    .FromAsync(
                        async () =>
                        {
                            await throttled
                                .InvokeAsync(a => a.SendEvent(e));

                            await Task.Delay(1000);
                        }
                    )
            )
            .Merge(1);

        await Task.Delay(10000);

        var info = await throttled.InvokeAsync(e => e.GetInfo());

        Assert.AreEqual(15, info.state.Count);
    }

    [Test]
    public async Task TestSingle()
    {
        var throttled = _actorSystem.GetActor<SimpleActor>("single");

        await Observable
            .Range(0, 5)
            .Select(
                e => Observable
                    .FromAsync(
                        async () =>
                        {
                            await throttled
                                .InvokeAsync(a => a.SendEvent(e));
                        }
                    )
            )
            .Merge(1);

        await Task.Delay(10000);

        var info = await throttled.InvokeAsync(e => e.GetInfo());

        Assert.True(info.state.SequenceEqual(info.current));
        Assert.AreEqual(info.state.Count, info.current.Count);
    }

    [Test]
    public async Task TestBufferNotPersisted()
    {
        var key = Guid.NewGuid()
            .ToString();

        var throttled = _actorSystem.GetActor<SimpleActor>(key);

        await Observable
            .Range(0, 15)
            .Select(
                e => Observable
                    .FromAsync(
                        async () =>
                        {
                            await throttled
                                .InvokeAsync(a => a.SendEvent(e));
                        }
                    )
            )
            .Merge(1);

        var info = await throttled.InvokeAsync(e => e.GetInfo());

        Assert.AreEqual(15, info.current.Count);
        Assert.AreEqual(0, info.state.Count);
    }

    [Test]
    public async Task TestBufferPersisted()
    {
        var key = Guid.NewGuid()
            .ToString();

        var throttled = _actorSystem.GetActor<SimpleActor>(key);

        await Observable
            .Range(0, 15)
            .Select(
                e => Observable
                    .FromAsync(
                        async () =>
                        {
                            await throttled
                                .InvokeAsync(a => a.SendEvent(e));
                        }
                    )
            )
            .Merge(1);

        await Task.Delay(10000);

        var info = await throttled.InvokeAsync(e => e.GetInfo());

        Assert.AreEqual(15, info.current.Count);
        Assert.AreEqual(15, info.state.Count);
        Assert.True(info.state.SequenceEqual(info.current));
    }

    [Test]
    public async Task TestForcePersist()
    {
        var throttled = _actorSystem.GetActor<SimpleActor>(
            Guid.NewGuid()
                .ToString()
        );

        await Observable
            .Range(0, 5)
            .Select(
                e => Observable
                    .FromAsync(
                        async () =>
                        {
                            await throttled
                                .InvokeAsync(a => a.SendEvent(e));
                        }
                    )
            )
            .Merge(1);

        await throttled.InvokeAsync(e => e.PersistForceAsync());

        await Task.Delay(200);

        var info = await throttled.InvokeAsync(e => e.GetInfo());

        Assert.True(info.state.SequenceEqual(info.current));
        Assert.AreEqual(info.state.Count, info.current.Count);
        Assert.AreEqual(5, info.state.Count);
    }

    public class SimpleActor : EventSourceThrottledActor<SimpleState>
    {
        private readonly ITimeProvider _timeProvider;

        public SimpleActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider, ITimeProvider timeProvider)
            : base(eventSourcePersistenceProvider)
        {
            _timeProvider = timeProvider;
        }

        protected override TimeSpan ThrottlingInterval => TimeSpan.FromSeconds(5);

        public async Task SendEvent(long num)
        {
            var ev = new SimpleEvent(Key, _timeProvider.UtcNow, num);
            await ApplySingleAsync(ev);
        }

        public Task<(List<long> state, List<long> current)> GetInfo()
        {
            return Task.FromResult((PersistedState.Nums, State.Nums));
        }
    }

    public class SimpleState : IApplicable
    {
        public List<long> Nums { get; private set; } = new List<long>();

        public void Apply(object ev)
        {
            switch (ev)
            {
                case SimpleEvent simpleEvent:
                    Nums.Add(simpleEvent.Num);

                    break;
            }
        }
    }

    public class SimpleEvent
    {
        public string Key { get; }
        public DateTime EventAt { get; }
        public long Num { get; }

        public SimpleEvent(string key, DateTime eventAt, long num)
        {
            Key = key;
            EventAt = eventAt;
            Num = num;
        }
    }
}