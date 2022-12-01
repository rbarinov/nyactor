using NYActor;
using NYActor.Cluster;

public static class NYExtensions
{
    private static IActorSystem _node;

    public static IServiceCollection AddActors(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IActorSystem>(
            e => _node
        );

        return serviceCollection;
    }

    public static IHost UseActorSystem(this IHost application, Action<ActorSystemBuilder> configureActorSystem)
    {
        var actorSystemBuilder = new ActorSystemBuilder();

        configureActorSystem(actorSystemBuilder);

        var node = actorSystemBuilder
            .Build(application.Services);

        _node = node;

        return application;
    }

    public static IHost UseClusterActorSystem(this IHost application, Action<ClusterActorSystemBuilder> configureActorSystem)
    {
        var actorSystemBuilder = new ClusterActorSystemBuilder();

        configureActorSystem(actorSystemBuilder);

        var node = actorSystemBuilder
            .Build(application.Services);

        _node = node;

        return application;
    }
}
