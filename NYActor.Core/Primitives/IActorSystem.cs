using NYActor.Core.Extensions;

namespace NYActor.Core
{
    public interface IActorSystem
    {
        IExpressionCallable<TActor> GetActor<TActor>(string key) where TActor : Actor;
        IExpressionCallable<TActor> GetActor<TActor>() where TActor : Actor;
    }
}
