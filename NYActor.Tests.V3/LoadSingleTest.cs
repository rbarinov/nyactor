using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NYActor.Tests.V3;

public class LoadSingleTest
{
    private const string Key = nameof(Key);

    private static Task QueryLoad(IActorSystem node, int i)
    {
        return Task.Run(
            () => node
                .GetActor<LoadActor>(Key + i)
                .InvokeAsync(c => c.Delay())
        );
    }

    [Test]
    public async Task TestLoad()
    {
        var node = new ActorSystemBuilder().BuildLocalActorNode();

        var reqs = Enumerable.Range(1, 2000)
            .Select(e => QueryLoad(node, e))
            .ToList();

        await Task.WhenAll(reqs);
    }

    public class LoadActor : Actor
    {
        private bool _isRunning;

        public Task Delay()
        {
            if (_isRunning) throw new InvalidOperationException();

            try
            {
                _isRunning = true;

                for (var i = 0; i < 10000000; i++)
                {
                }

                return Task.CompletedTask;
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
