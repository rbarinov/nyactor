using Microsoft.Extensions.DependencyInjection;

namespace NYActor.Cluster;

public static class ClusterActorSystemBuilderExtensions
{
    public static IServiceCollection AddClusterActorSystem(
        this IServiceCollection serviceCollection,
        Action<ActorSystemBuilder>? configureActorSystem = null
    )
    {
        var actorSystemBuilder = new ClusterActorSystemBuilder();
        configureActorSystem?.Invoke(actorSystemBuilder);

        serviceCollection.AddSingleton<IActorSystem>(
            e => actorSystemBuilder
                .WithServiceProvider(e)
                .Build()
        );

        return serviceCollection;
    }
}