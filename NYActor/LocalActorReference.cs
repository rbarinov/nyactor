using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace NYActor;

// @todo make internal
public class LocalActorReference<TActor> : IActorReference<TActor>
    where TActor : IActor
{
    private readonly Func<ILocalActorDispatcher<TActor>> _dispatcherGetter;

    public LocalActorReference(Func<ILocalActorDispatcher<TActor>> dispatcherGetter)
    {
        _dispatcherGetter = dispatcherGetter;
    }

    public Task SendAsync<TMessage>(
        TMessage message,
        ActorExecutionContext actorExecutionContext = null
    )
    {
        return _dispatcherGetter()
            .SendAsync(
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

        return _dispatcherGetter()
            .InvokeAsync(
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

        return _dispatcherGetter()
            .InvokeAsync(
                func,
                callName,
                actorExecutionContext
            );
    }

    public IActorReference<TBaseActor> ToBaseRef<TBaseActor>() where TBaseActor : IActor
    {
        var baseActorDispatcher = _dispatcherGetter() as ILocalActorDispatcher<TBaseActor>;

        return new LocalActorReference<TBaseActor>(() => _dispatcherGetter() as ILocalActorDispatcher<TBaseActor>);
    }
}
