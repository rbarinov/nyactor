using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.CompilerServices;
using SimpleInjector;

namespace NYActor.Core
{
    public class Node : IActorSystem, IDisposable
    {
        private readonly Subject<(string, MessageQueueItem)> _messageQueue = new Subject<(string, MessageQueueItem)>();
        internal IObserver<(string actorPath, MessageQueueItem messageQueueItem)> MessageQueueObserver => _messageQueue;

        private readonly ConcurrentDictionary<string, ActorWrapperBase> _actors =
            new ConcurrentDictionary<string, ActorWrapperBase>();

        private readonly Container _container;

        public TimeSpan DefaultActorDeactivationInterval = TimeSpan.FromMinutes(20);
        public TimeSpan DefaultActorDeactivationThrottleInterval = TimeSpan.FromSeconds(10);

        public Node()
        {
            _container = new Container();

            _messageQueue
                .ObserveOn(ThreadPoolScheduler.Instance)
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Where(e => _actors.ContainsKey(e.Item1))
                .GroupBy(e => e.Item1)
                .SelectMany(g => g
                    .Select(e => Observable.FromAsync(() => _actors[g.Key].OnMessageEnqueued(e.Item2),
                        ThreadPoolScheduler.Instance))
                    .Merge(1)
                )
                .Subscribe();
        }

        public Node RegisterActorsFromAssembly(Assembly assembly)
        {
            assembly.GetTypes()
                .Where(e => e.IsSubclassOf(typeof(Actor)))
                .ToList()
                .ForEach(e => _container.Register(e));

            return this;
        }

        public Node ConfigureInjector(Action<Container> configure)
        {
            _container.RegisterInstance<IActorSystem>(this);

            configure(_container);

            return this;
        }

        public Node OverrideDefaultDeactivationInterval(TimeSpan actorDeactivationInterval)
        {
            DefaultActorDeactivationInterval = actorDeactivationInterval;
            return this;
        }

        public ActorWrapper<TActor> GetActor<TActor>(string key) where TActor : Actor
        {
            var actorPath = $"{typeof(TActor).FullName}-{key}";

            var actorWrapperBase = _actors.GetOrAdd(actorPath, e => new ActorWrapper<TActor>(e, key, this, _container));
            var actorWrapper = actorWrapperBase as ActorWrapper<TActor>;

            return actorWrapper;
        }

        public ActorWrapper<TActor> GetActor<TActor>() where TActor : Actor =>
            GetActor<TActor>(Guid.NewGuid().ToString().ToLower());

        public void Dispose()
        {
            _messageQueue.OnCompleted();
            _messageQueue.Dispose();
            _container?.Dispose();

            foreach (var wrapperBase in _actors)
            {
                wrapperBase.Value.Dispose();
            }
        }
    }
}