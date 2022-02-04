using System.Collections.Concurrent;

namespace NYActor;

public class ActorNode : IActorSystem, IDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<IActorDispatcherInternal>> _actorDispatchers = new();
    private readonly IServiceProvider _serviceProvider;

    private readonly Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
        tracingActivity)> _tracingActivityFactory;

    public ActorNode(
        IServiceProvider serviceProvider,
        TimeSpan actorDeactivationTimeout,
        Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
            tracingActivity)> tracingActivityFactory
    )
    {
        _serviceProvider = serviceProvider;
        _tracingActivityFactory = tracingActivityFactory;
        ActorDeactivationTimeout = actorDeactivationTimeout;
    }

    internal TimeSpan ActorDeactivationTimeout { get; }

    public virtual IActorReference<TActor> GetActor<TActor>(string key) where TActor : Actor
    {
        var actorPath = $"{typeof(TActor).FullName}-{key}";

        Lazy<IActorDispatcherInternal> lazyWrapper;

        lock (_actorDispatchers)
        {
            lazyWrapper = _actorDispatchers.GetOrAdd(
                actorPath,
                e => new Lazy<IActorDispatcherInternal>(
                    () =>
                        new ActorDispatcher<TActor>(key, this, _serviceProvider)
                )
            );
        }

        var actorWrapper = lazyWrapper.Value as IActorDispatcherInternal<TActor>;

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
