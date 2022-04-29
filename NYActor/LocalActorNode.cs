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

        Lazy<ILocalActorDispatcher> lazyWrapper;

        lock (_actorDispatchers)
        {
            lazyWrapper = _actorDispatchers.GetOrAdd(
                actorPath,
                e => new Lazy<ILocalActorDispatcher>(
                    () =>
                        new LocalActorDispatcher<TActor>(
                            key,
                            _actorSystemGetter?.Invoke() ?? this,
                            _serviceProvider,
                            _tracingActivityFactory,
                            ActorDeactivationTimeout
                        )
                )
            );
        }

        var actorWrapper = lazyWrapper.Value as ILocalActorDispatcher<TActor>;

        return new LocalActorReference<TActor>(actorWrapper);
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