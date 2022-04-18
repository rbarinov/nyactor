using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace NYActor;

// @todo make internal
public class LocalActorReference<TActor> : IActorReference<TActor>
    where TActor : IActor
{
    private readonly ILocalActorDispatcher<TActor> _localActorDispatcher;

    public LocalActorReference(ILocalActorDispatcher<TActor> localActorDispatcher)
    {
        _localActorDispatcher = localActorDispatcher;
    }

    public Task SendAsync<TMessage>(
        TMessage message,
        ActorExecutionContext actorExecutionContext = null
    )
    {
        return _localActorDispatcher.SendAsync(
            message,
            actorExecutionContext
        );
    }

    public Task<TResult> InvokeAsync<TResult>(
        Expression<Func<TActor, Task<TResult>>> req,
        ActorExecutionContext actorExecutionContext = null
    )
    {
        var callName = Regex.Match(req.Body.ToString(), @"([a-zA-Z0-9_]+)\(.+")
            .Groups[1]
            .Value;

        var func = req.Compile();

        return _localActorDispatcher.InvokeAsync(
            func,
            callName,
            actorExecutionContext
        );
    }

    public Task InvokeAsync(Expression<Func<TActor, Task>> req, ActorExecutionContext actorExecutionContext = null)
    {
        var callName = Regex.Match(req.Body.ToString(), @"([a-zA-Z0-9_]+)\(.+")
            .Groups[1]
            .Value;

        var func = req.Compile();

        return _localActorDispatcher.InvokeAsync(
            func,
            callName,
            actorExecutionContext
        );
    }

    public IActorReference<TBaseActor> ToBaseRef<TBaseActor>() where TBaseActor : IActor
    {
        var baseActorDispatcher = _localActorDispatcher as ILocalActorDispatcher<TBaseActor>;

        return new LocalActorReference<TBaseActor>(baseActorDispatcher);
    }
}
