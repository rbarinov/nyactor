using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NYActor.Core;

namespace NYActor.Tests
{
    public class ActorExecutesInThreadPoolTest
    {
        [Test]
        public async Task ThreadsTest()
        {
            using var node = new Node();

            var tasks = Enumerable.Range(1, 100)
                .Select(e => node.GetActor<HeavyDutyActor>().InvokeAsync(c => c.GetThread()))
                .ToList();

            await Task.WhenAll(tasks);

            var threadIds = tasks.Select(e => e.Result).Distinct().ToList();

            Assert.True(threadIds.Distinct().Count() > 1);
        }

        public class HeavyDutyActor : Actor
        {
            public Task<int> GetThread()
            {
                var j = 0;
                for (var i = 0; i < 100000000; i++)
                {
                    j++;
                }

                return Task.FromResult(Thread.CurrentThread.ManagedThreadId);
            }
        }
    }
}