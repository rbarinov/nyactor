using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
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
                .Where(e => _actors.ContainsKey(e.Item1))
                .GroupBy(e => e.Item1)
                .SelectMany(g => g
                    .Select(e => Observable.FromAsync(() => _actors[g.Key].OnMessageEnqueued(e.Item2)))
                    .Concat()
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

            ActorWrapper<TActor> actorWrapper;

            if (!_actors.TryGetValue(actorPath, out var actor))
            {
                actor = new ActorWrapper<TActor>(actorPath, key, this, _container);
                actorWrapper = _actors.GetOrAdd(actorPath, actor) as ActorWrapper<TActor>;
            }
            else
            {
                actorWrapper = actor as ActorWrapper<TActor>;
            }

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