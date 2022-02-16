using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NYActor.EventSourcing;
using NYActor.EventSourcing.EventStore.v5;
using NYActor.EventSourcing.S3;
using NYActor.Patterns.Throttled;

namespace NYActor.Tests.V3;

[TestFixture]
public class EventSourceThrottledTests
{
    private IActorSystem _actorSystem;

    private IActorReference<SimpleActor> GetActor(string store, string key)
    {
        return store switch
        {
            "es" => _actorSystem.GetActor<EsActor>(key)
                .ToBaseRef<SimpleActor>(),
            "s3" => _actorSystem.GetActor<S3Actor>(key)
                .ToBaseRef<SimpleActor>(),
            _ => throw new ArgumentOutOfRangeException(nameof(store))
        };
    }

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

        var s3AccessKeyId = Environment.GetEnvironmentVariable("S3AccessKeyId");
        var s3SecretAccessKey = Environment.GetEnvironmentVariable("S3SecretAccessKey");
        var s3EndpointUrl = Environment.GetEnvironmentVariable("S3EndpointUrl");
        var s3Space = Environment.GetEnvironmentVariable("S3Space");

        var client = new AmazonS3Client(
            s3AccessKeyId,
            s3SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL = s3EndpointUrl
            }
        );

        var directory = "nyactor-tests";

        _actorSystem = new ActorSystemBuilder()
            .ConfigureServices(
                e =>
                {
                    e.AddSingleton<ITimeProvider>(new NaturalTimeProvider());

                    e.AddSingleton<IEventStoreV5EventSourcePersistenceProvider>(
                        new EventStoreV5EventSourcePersistenceProvider(eventStoreConnection, 500)
                    );

                    e.AddSingleton<IS3EventSourcePersistenceProvider>(
                        new S3EventSourcePersistenceProvider(client, s3Space, directory)
                    );
                }
            )
            .Build();
    }

    [Test]
    [TestCase("es")]
    [TestCase("s3")]
    [Parallelizable(ParallelScope.All)]
    public async Task Test(string store)
    {
        var key = Guid.NewGuid()
            .ToString();

        var throttled = GetActor(store, key);

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
    [TestCase("es")]
    [TestCase("s3")]
    [Parallelizable(ParallelScope.All)]
    public async Task TestSingle(string store)
    {
        var throttled = GetActor(store, "single");

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
    [TestCase("es")]
    [TestCase("s3")]
    [Parallelizable(ParallelScope.All)]
    public async Task TestBufferNotPersisted(string store)
    {
        var key = Guid.NewGuid()
            .ToString();

        var throttled = GetActor(store, key);

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
    [TestCase("es")]
    [TestCase("s3")]
    [Parallelizable(ParallelScope.All)]
    public async Task TestBufferPersisted(string store)
    {
        var key = Guid.NewGuid()
            .ToString();

        var throttled = GetActor(store, key);

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
    [TestCase("es")]
    [TestCase("s3")]
    [Parallelizable(ParallelScope.All)]
    public async Task TestForcePersist(string store)
    {
        var throttled = GetActor(
            store,
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

    private class S3Actor : SimpleActor
    {
        public S3Actor(IS3EventSourcePersistenceProvider eventSourcePersistenceProvider, ITimeProvider timeProvider)
            : base(eventSourcePersistenceProvider, timeProvider)
        {
        }
    }

    private class EsActor : SimpleActor
    {
        public EsActor(
            IEventStoreV5EventSourcePersistenceProvider eventSourcePersistenceProvider,
            ITimeProvider timeProvider
        )
            : base(eventSourcePersistenceProvider, timeProvider)
        {
        }
    }

    private abstract class SimpleActor : EventSourceThrottledActor<SimpleState>
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