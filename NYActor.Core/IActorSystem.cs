using System;
using System.Threading.Tasks;

namespace NYActor.Core
{
    public interface IActorSystem
    {
        ActorWrapper<TActor> GetActor<TActor>(string key) where TActor : Actor;
        ActorWrapper<TActor> GetActor<TActor>() where TActor : Actor;
    }
}