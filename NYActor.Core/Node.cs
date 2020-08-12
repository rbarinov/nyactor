using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using SimpleInjector;

namespace NYActor.Core
{
    public class Node : IActorSystem, IDisposable
    {
        private readonly Container _container;

        private readonly ConcurrentDictionary<string, Lazy<object>> _actorWrappers =
            new ConcurrentDictionary<string, Lazy<object>>();

        public TimeSpan DefaultActorDeactivationTimeout = TimeSpan.FromMinutes(20);

        public Node()
        {
            _container = new Container();
        }

        public IActorWrapper<TActor> GetActor<TActor>(string key) where TActor : Actor
        {
            var actorPath = $"{typeof(TActor).FullName}-{key}";

            var lazyWrapper = _actorWrappers.GetOrAdd(
                actorPath,
                e => new Lazy<object>(
                    () =>
                        new GenericActorWrapper<TActor>(key, this, _container)
                )
            );

            var actorWrapper = lazyWrapper.Value as IActorWrapper<TActor>;

            return actorWrapper;
        }

        public IActorWrapper<TActor> GetActor<TActor>() where TActor : Actor =>
            GetActor<TActor>(
                Guid.NewGuid()
                    .ToString()
                    .ToLower()
            );

        public void Dispose()
        {
            foreach (var wrapperBase in _actorWrappers)
            {
                (wrapperBase.Value.Value as IDisposable)?.Dispose();
            }

            _container?.Dispose();
        }

        public Node RegisterActorsFromAssembly(Assembly assembly)
        {
            assembly.GetTypes()
                .Where(
                    e => e.IsSubclassOf(typeof(Actor)) &&
                         !e.IsAbstract &&
                         e.GetConstructors()
                             .Any(c => c.IsPublic)
                         && !e.IsNotPublic
                )
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

        public Node OverrideDefaultDeactivationTimeout(TimeSpan actorDeactivationInterval)
        {
            DefaultActorDeactivationTimeout = actorDeactivationInterval;

            return this;
        }
    }
}
