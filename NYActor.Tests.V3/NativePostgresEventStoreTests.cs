using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.EventSourcing;
using NYActor.EventSourcing.PostgresqlNative;

namespace NYActor.Tests.V3;

public class NativePostgresEventStoreTests
{
    [Test]
    public async Task InitWorksGood()
    {
        var provider = new PostgresqlEventSourcePersistenceProvider(
            "Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=password;Minimum Pool Size=5;Maximum Pool Size=10;Keepalive=10;",
            "test"
        );

        await provider.InitDbAsync();
        Assert.Pass();
    }

    [Test]
    public async Task BinaryImport()
    {
        var provider = new BinaryImportPostgresqlEventSourcePersistenceProvider(
            "Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=password;Minimum Pool Size=5;Maximum Pool Size=10;Keepalive=10;",
            "test"
        );

        await provider.InitDbAsync();

        var scope = await provider.OpenBinaryImportScope();

        foreach (var i in Enumerable.Range(1, 1000))
        {
            await provider.PersistEventsAsync(
                typeof(NativePostgresEventStoreTests),
                "key-" + i,
                -1,
                new List<EventSourceEventData>
                {
                    new EventSourceEventData("t1", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t2", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t3", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t4", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t5", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t6", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t7", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t8", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t9", Encoding.UTF8.GetBytes("data")),
                    new EventSourceEventData("t10", Encoding.UTF8.GetBytes("data"))
                }
            );
        }

        await scope.DisposeAsync();

        Assert.Pass();
    }
}
