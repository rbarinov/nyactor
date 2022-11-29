using System.Collections.Concurrent;

namespace NYActor;

public class LocalActorNode : IActorSystem, IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<ILocalActorDispatcher>> _actorDispatchers = new();
    private readonly IServiceProvider _serviceProvider;

    private readonly Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
        tracingActivity)> _tracingActivityFactory;

    private readonly Func<IActorSystem> _actorSystemGetter;

    public LocalActorNode(
        IServiceProvider serviceProvider,
        TimeSpan actorDeactivationTimeout,
        Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
            tracingActivity)> tracingActivityFactory,
        Func<IActorSystem> actorSystemGetter = null
    )
    {
        _serviceProvider = serviceProvider;
        _tracingActivityFactory = tracingActivityFactory;
        _actorSystemGetter = actorSystemGetter;
        ActorDeactivationTimeout = actorDeactivationTimeout;
    }

    internal TimeSpan ActorDeactivationTimeout { get; }

    public virtual IActorReference<TActor> GetActor<TActor>(string key) where TActor : Actor
    {
        var actorPath = $"{typeof(TActor).FullName}-{key}";
        var actorSystem = _actorSystemGetter?.Invoke() ?? this;
        var serviceProvider = _serviceProvider;
        var tracingActivityFactory = _tracingActivityFactory;
        var actorDeactivationTimeout = ActorDeactivationTimeout;

        var onDeactivation = () =>
        {
            _actorDispatchers.TryRemove(actorPath, out var deleted);

            if (deleted?.IsValueCreated == true)
            {
                var localActorDispatcher = deleted.Value;

                if (localActorDispatcher is LocalActorDispatcher<TActor>)
                {
                    ((LocalActorDispatcher<TActor>) localActorDispatcher).Dispose();
                }
            }
        };

        Func<ILocalActorDispatcher<TActor>> dipatcherGetter = () =>
        {
            Lazy<ILocalActorDispatcher> lazyWrapper;

            lock (_actorDispatchers)
            {
                lazyWrapper = _actorDispatchers.GetOrAdd(
                    actorPath,
                    e => new Lazy<ILocalActorDispatcher>(
                        () =>
                        {
                            return new LocalActorDispatcher<TActor>(
                                key,
                                actorSystem,
                                serviceProvider,
                                tracingActivityFactory,
                                actorDeactivationTimeout,
                                onDeactivation
                            );
                        }
                    )
                );
            }

            var actorDispatcher = lazyWrapper.Value as ILocalActorDispatcher<TActor>;

            return actorDispatcher;
        };

        return new LocalActorReference<TActor>(dipatcherGetter);
    }

    public void Dispose()
    {
        foreach (var wrapperBase in _actorDispatchers) (wrapperBase.Value.Value as IDisposable)?.Dispose();
    }

    internal (ActorExecutionContext actorExecutionContext, ITracingActivity tracingActivity) CreateTracingActivity(
        ActorExecutionContext actorExecutionContext,
        string activityName
    )
    {
        if (_tracingActivityFactory == null) return (actorExecutionContext, default);

        return _tracingActivityFactory(actorExecutionContext, activityName);
    }
}
