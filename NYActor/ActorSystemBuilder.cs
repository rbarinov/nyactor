using Microsoft.Extensions.DependencyInjection;

namespace NYActor;

public sealed class ActorSystemBuilder
{
    public static readonly TimeSpan DefaultActorDeactivationTimeout = TimeSpan.FromMinutes(20);
    private TimeSpan _actorDeactivationTimeout = DefaultActorDeactivationTimeout;

    private IServiceProvider _serviceProvider;

    private Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
        tracingActivity)> _tracingActivityFactory;

    public ActorSystemBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        var serviceCollection = new ServiceCollection();

        configureServices(serviceCollection);

        _serviceProvider = serviceCollection.BuildServiceProvider();

        return this;
    }

    public ActorSystemBuilder WithActorDeactivationTimeout(TimeSpan actorDeactivationTimeout)
    {
        _actorDeactivationTimeout = actorDeactivationTimeout;

        return this;
    }

    public ActorSystemBuilder AddGenericTracing(
        Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
            tracingActivity)> tracingActivityFactory
    )
    {
        _tracingActivityFactory = tracingActivityFactory;

        return this;
    }

    public IActorSystem BuildLocalActorNode()
    {
        var actorNode = new LocalActorNode(_serviceProvider, _actorDeactivationTimeout, _tracingActivityFactory);

        return actorNode;
    }

    public IActorSystem BuildCluster()
    {
        var clusterActorNode = new ClusterActorNode(
            _serviceProvider,
            _actorDeactivationTimeout,
            _tracingActivityFactory
        );

        return clusterActorNode;
    }
}
