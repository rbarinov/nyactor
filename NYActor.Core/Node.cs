using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NYActor.Core.Extensions;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SimpleInjector;

namespace NYActor.Core
{
    public class Node : IActorSystem, IDisposable
    {
        private readonly Container _container;

        private readonly ConcurrentDictionary<string, Lazy<object>> _actorWrappers =
            new ConcurrentDictionary<string, Lazy<object>>();

        public TimeSpan DefaultActorDeactivationTimeout = TimeSpan.FromMinutes(20);
        public bool TracingEnabled = false;

        public Node()
        {
            _container = new Container();
        }

        public virtual IExpressionCallable<TActor> GetActor<TActor>(string key) where TActor : Actor
        {
            var actorPath = $"{typeof(TActor).FullName}-{key}";

            Lazy<object> lazyWrapper;

            lock (_actorWrappers)
            {
                lazyWrapper = _actorWrappers.GetOrAdd(
                    actorPath,
                    e => new Lazy<object>(
                        () =>
                            new GenericActorWrapper<TActor>(key, this, _container)
                    )
                );
            }

            var actorWrapper = lazyWrapper.Value as IActorWrapper<TActor>;

            return new ExpressionCallable<TActor>(actorWrapper);
        }

        public IExpressionCallable<TActor> GetActor<TActor>() where TActor : Actor =>
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

        public Node RegisterTraceProvider(string host, int port)
        {
            var assembly = Assembly.GetEntryAssembly()
                ?.GetName()
                ?.Name
                ?.ToLowerInvariant();

            var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(assembly)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: assembly)
                )
                .AddJaegerExporter(
                    o =>
                    {
                        o.AgentHost = host;
                        o.AgentPort = port;
                    }
                )
                .Build();

            _container.RegisterInstance(tracerProvider);
            _container.RegisterInstance(new ActivitySource(assembly));

            TracingEnabled = true;

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