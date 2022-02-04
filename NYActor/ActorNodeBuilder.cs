using Microsoft.Extensions.DependencyInjection;

namespace NYActor;

public class ActorNodeBuilder
{
    public static readonly TimeSpan DefaultActorDeactivationTimeout = TimeSpan.FromMinutes(20);
    private TimeSpan _actorDeactivationTimeout = DefaultActorDeactivationTimeout;

    private IServiceProvider _serviceProvider;

    private Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
        tracingActivity)> _tracingActivityFactory;

    public ActorNodeBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        var serviceCollection = new ServiceCollection();

        configureServices(serviceCollection);

        _serviceProvider = serviceCollection.BuildServiceProvider();

        return this;
    }

    public ActorNodeBuilder WithActorDeactivationTimeout(TimeSpan actorDeactivationTimeout)
    {
        _actorDeactivationTimeout = actorDeactivationTimeout;

        return this;
    }

    public ActorNodeBuilder AddGenericTracing(
        Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
            tracingActivity)> tracingActivityFactory
    )
    {
        _tracingActivityFactory = tracingActivityFactory;

        return this;
    }

    public ActorNode Build()
    {
        var actorNode = new ActorNode(_serviceProvider, _actorDeactivationTimeout, _tracingActivityFactory);

        return actorNode;
    }
}
