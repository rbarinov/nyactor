using Microsoft.Extensions.DependencyInjection;

namespace NYActor;

public class ActorSystemBuilder
{
    public static readonly TimeSpan DefaultActorDeactivationTimeout = TimeSpan.FromMinutes(20);

    protected IServiceCollection ServiceCollection;
    protected IServiceProvider ServiceProvider;

    protected TimeSpan ActorDeactivationTimeout = DefaultActorDeactivationTimeout;

    protected Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
        tracingActivity)> TracingActivityFactory;

    public ActorSystemBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        ServiceCollection ??= new ServiceCollection();
        configureServices?.Invoke(ServiceCollection);
        ServiceProvider = ServiceCollection.BuildServiceProvider();

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

    public ActorSystemBuilder WithServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;

        return this;
    }

    public virtual IActorSystem Build()
    {
        var actorNode = new LocalActorNode(
            ServiceProvider,
            ActorDeactivationTimeout,
            TracingActivityFactory
        );

        return actorNode;
    }
}