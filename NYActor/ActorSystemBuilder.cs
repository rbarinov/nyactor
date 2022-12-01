using Microsoft.Extensions.DependencyInjection;

namespace NYActor;

public class ActorSystemBuilder
{
    public static readonly TimeSpan DefaultActorDeactivationTimeout = TimeSpan.FromMinutes(20);

    protected TimeSpan ActorDeactivationTimeout = DefaultActorDeactivationTimeout;

    protected Func<ActorExecutionContext, string, (ActorExecutionContext actorExecutionContext, ITracingActivity
        tracingActivity)> TracingActivityFactory;

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

    public virtual IActorSystem Build(IServiceProvider serviceProvider)
    {
        var actorNode = new LocalActorNode(serviceProvider, ActorDeactivationTimeout, TracingActivityFactory);

        return actorNode;
    }
}
