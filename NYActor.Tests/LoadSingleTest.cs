using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Core;

namespace NYActor.Tests
{
    public class LoadSingleTest
    {
        private static Task QueryLoad(IActorSystem node) => Task.Factory
            .StartNew(() => node
                .GetActor<LoadActor>()
                .InvokeAsync(c => c.Delay())
            )
            .Unwrap();

        [Test]
        public async Task TestLoad()
        {
            var node = new Node();

            var reqs = Enumerable.Range(1, 20000)
                .Select(e => QueryLoad(node))
                .ToList();

            await Task.WhenAll(reqs);
        }

        public class LoadActor : Actor
        {
            private bool _isRunning = false;

            public Task Delay()
            {
                if (_isRunning) throw new InvalidOperationException();
                try
                {
                    _isRunning = true;
                    for (int i = 0; i < 10000000; i++)
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
}