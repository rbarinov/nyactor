using Microsoft.Extensions.DependencyInjection;

namespace NYActor;

public static class ActorSystemBuilderExtensions
{
    public static IServiceCollection AddActorSystem(
        this IServiceCollection serviceCollection,
        Action<ActorSystemBuilder>? configureActorSystem = null
    )
    {
        var actorSystemBuilder = new ActorSystemBuilder();
        configureActorSystem?.Invoke(actorSystemBuilder);

        serviceCollection.AddSingleton<IActorSystem>(
            e => actorSystemBuilder
                .WithServiceProvider(e)
                .Build()
        );

        return serviceCollection;
    }
}