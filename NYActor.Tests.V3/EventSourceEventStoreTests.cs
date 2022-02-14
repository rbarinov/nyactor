using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using NYActor.EventSourcing;
using NYActor.EventSourcing.EventStore.v5;
using NYActor.EventSourcing.Projections.Postgres;
using NYActor.Patterns.LongSequence;
using NYActor.Patterns.StsMapping;

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

        var postgresConnectionFactory = new PostgresConnectionFactoryBuilder()
            .WithConnectionString(
                new NpgsqlConnectionStringBuilder
                    {
                        Database = "postgres",
                        Username = "postgres",
                        Password = "postgres",
                        Host = "localhost",
                        Port = 5432,
                        MinPoolSize = 5,
                        MaxPoolSize = 20
                    }
                    .ToString()
            )
            .Build();

        var actorSystem = new ActorSystemBuilder()
            .ConfigureServices(
                e =>
                {
                    e.AddSingleton<IEventStoreV5EventSourcePersistenceProvider>(
                        new EventStoreV5EventSourcePersistenceProvider(eventStoreConnection, 500)
                    );

                    e.AddSingleton<IPostgresConnectionFactory>(postgresConnectionFactory);

                    e.AddSingleton<ITimeProvider>(new NaturalTimeProvider());
                }
            )
            .Build();

        var seq = actorSystem.GetActor<LongSequenceActor>("my-seq");

        var id = await seq.InvokeAsync(e => e.GetLastId());
        id = await seq.InvokeAsync(e => e.Generate());
        id = await seq.InvokeAsync(e => e.GetLastId());

        var sts = actorSystem.GetActor<StsMappingActor>("my-sts");
        var stsInfo = await sts.InvokeAsync(e => e.GetInfo());
        await sts.InvokeAsync(e => e.Attach("their-sts"));
        stsInfo = await sts.InvokeAsync(e => e.GetInfo());
    }
}
