using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;
using NYActor.EventSourcing.EventStore.v5;

namespace NYActor.Tests.V3;

public class EventSourceEventStoreTests
{
    [Test]
    public async Task Test()
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

        var persistenceProvider = new EventStoreV5EventSourcePersistenceProvider(eventStoreConnection, 256);

        // Observable.Interval(TimeSpan.FromSeconds(5))
        //     .Select(
        //         e => Observable.FromAsync(
        //             () => persistenceProvider.PersistEventsAsync(
        //                 typeof(TestPersistedActor),
        //                 "none",
        //                 e - 1,
        //                 new[] {new TestEvent {Val = e}}
        //             )
        //         )
        //     )
        //     .Merge(1)
        //     .Subscribe();

        var observable = await persistenceProvider.ObserveAllEvents(default);
    }
}

public class TestEvent
{
    public long Val { get; set; }
}

public class TestPersistedActor : Actor
{
}
