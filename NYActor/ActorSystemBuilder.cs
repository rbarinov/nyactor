using Microsoft.Extensions.DependencyInjection;

namespace NYActor;

public class ActorSystemBuilder
{
    public static readonly TimeSpan DefaultActorDeactivationTimeout = TimeSpan.FromMinutes(20);

    protected TimeSpan ActorDeactivationTimeout = DefaultActorDeactivationTimeout;

    protected IServiceProvider ServiceProvider;

    protected Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
        tracingActivity)> TracingActivityFactory;

    public ActorSystemBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        var serviceCollection = new ServiceCollection();

        configureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        return this;
    }

    public ActorSystemBuilder WithActorDeactivationTimeout(TimeSpan actorDeactivationTimeout)
    {
        ActorDeactivationTimeout = actorDeactivationTimeout;

        return this;
    }

    public ActorSystemBuilder AddGenericTracing(
        Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
            tracingActivity)> tracingActivityFactory
    )
    {
        TracingActivityFactory = tracingActivityFactory;

        return this;
    }

    public virtual IActorSystem Build()
    {
        var actorNode = new LocalActorNode(ServiceProvider, ActorDeactivationTimeout, TracingActivityFactory);

        return actorNode;
    }
}
