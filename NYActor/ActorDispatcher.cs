using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using NYActor.Message;

namespace NYActor;

public class ActorDispatcher<TActor> : IActorDispatcherInternal<TActor>, IDisposable where TActor : Actor
{
    private readonly string _fullName;
    private readonly Subject<object> _ingress = new();
    private readonly string _key;
    private readonly IServiceProvider _serviceProvider;

    private readonly Subject<Unit> _unsubscribeAll = new();
    private TActor _actor;

    private Subject<Unit> _deactivationWatchdogSubject;
    private IDisposable _deactivationWatchdogSubscription;

    public ActorDispatcher(string key, ActorNode actorNode, IServiceProvider serviceProvider)
    {
        _key = key;
        _serviceProvider = serviceProvider;
        ActorNode = actorNode;
        _fullName = $"{typeof(TActor).FullName}-{_key}";

        _ingress
            .ObserveOn(ThreadPoolScheduler.Instance)
            .Select(e => Observable.FromAsync(() => HandleIngressMessage(e)))
            .Merge(1)
            .TakeUntil(_unsubscribeAll)
            .Subscribe();
    }

    internal ActorExecutionContext CurrentExecutionContext { get; private set; }

    public ActorNode ActorNode { get; }

    public void DelayDeactivation(TimeSpan deactivationTimeout)
    {
        SubscribeDeactivationWatchdog(deactivationTimeout);
    }

    public Task SendAsync<TMessage>(TMessage message, ActorExecutionContext actorExecutionContext = null)
    {
        var messageWrapper = new IngressOnewayMessage(message, actorExecutionContext);
        _ingress.OnNext(messageWrapper);

        return Task.CompletedTask;
    }

    public async Task<TResult> InvokeAsync<TResult>(
        Func<TActor, Task<TResult>> req,
        string callName,
        ActorExecutionContext actorExecutionContext = null
    )
    {
        var taskCompletionSource = new TaskCompletionSource<object>();

        _ingress.OnNext(
            new IngressAskMessage(
                async e => await req((TActor) e),
                taskCompletionSource,
                callName,
                actorExecutionContext
            )
        );

        var response = await taskCompletionSource.Task.ConfigureAwait(false);

        return (TResult) response;
    }

    public async Task InvokeAsync(
        Func<TActor, Task> req,
        string callName,
        ActorExecutionContext actorExecutionContext = null
    )
    {
        var taskCompletionSource = new TaskCompletionSource<object>();

        _ingress.OnNext(
            new IngressAskMessage(
                async e =>
                {
                    await req((TActor) e);

                    return Unit.Default;
                },
                taskCompletionSource,
                callName,
                actorExecutionContext
            )
        );

        await taskCompletionSource.Task.ConfigureAwait(false);
    }

    public void Dispose()
    {
        _unsubscribeAll.OnNext(Unit.Default);
        _unsubscribeAll.OnCompleted();
    }

    private async Task HandleIngressMessage(object message)
    {
        if (message is PoisonPill or IngressOnewayMessage {Payload: PoisonPill})
        {
            if (_actor == null) return;

            try
            {
                await DeactivateActorInstance()
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // @todo handle deactivation unhandled exception
            }
        }

        if (message is not IngressMessage ingressMessage) return;

        var (actorExecutionContext, tracingActivity) = ActorNode.CreateTracingActivity(
            ingressMessage.ActorExecutionContext,
            $"{_fullName}: {(ingressMessage as IngressAskMessage)?.CallName ?? nameof(IngressOnewayMessage)}"
        );

        CurrentExecutionContext = actorExecutionContext;

        try
        {
            if (_actor == null)
                try
                {
                    await ActivateActorInstance()
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // @todo handle activation unhandled exception
                }

            ThrottleDeactivation();

            if (ingressMessage is IngressOnewayMessage ingressOnewayMessage)
                try
                {
                    await _actor.OnMessage(ingressOnewayMessage.Payload);
                }
                catch (Exception)
                {
                    try
                    {
                        await DeactivateActorInstance()
                            .ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // @todo handle deactivation unhandled exception
                    }
                }

            if (ingressMessage is IngressAskMessage ingressAskMessage)
                try
                {
                    var response = await ingressAskMessage.Invoke(_actor)
                        .ConfigureAwait(false);

                    ingressAskMessage.TaskCompletionSource.TrySetResult(response);
                }
                catch (Exception ex)
                {
                    try
                    {
                        await DeactivateActorInstance()
                            .ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // @todo handle deactivation unhandled exception
                    }

                    ingressAskMessage.TaskCompletionSource.TrySetException(ex);

                    tracingActivity?.SetError(ex, ex.Message);
                }

            ThrottleDeactivation();
        }
        finally
        {
            tracingActivity?.Dispose();
            CurrentExecutionContext = null;
        }
    }

    private async Task ActivateActorInstance()
    {
        _actor = ActivatorUtilities.CreateInstance<TActor>(_serviceProvider);

        _actor.InitializeInstanceFields(
            _key,
            this
        );

        SubscribeDeactivationWatchdog();

        await _actor.Activate()
            .ConfigureAwait(false);
    }

    private void SubscribeDeactivationWatchdog(TimeSpan? deactivationTimeout = null)
    {
        UnsubscribeDeactivationWatchdog();

        _deactivationWatchdogSubject = new Subject<Unit>();

        _deactivationWatchdogSubscription = _deactivationWatchdogSubject
            .Timeout(deactivationTimeout ?? ActorNode.ActorDeactivationTimeout)
            .IgnoreElements()
            .Subscribe(_ => { }, timeout => _ingress.OnNext(PoisonPill.Default));
    }

    private void UnsubscribeDeactivationWatchdog()
    {
        _deactivationWatchdogSubscription?.Dispose();
        _deactivationWatchdogSubscription = null;

        _deactivationWatchdogSubject?.Dispose();
        _deactivationWatchdogSubject = null;
    }

    private void ThrottleDeactivation()
    {
        _deactivationWatchdogSubject?.OnNext(Unit.Default);
    }

    private async Task DeactivateActorInstance()
    {
        if (_actor != null)
            try
            {
                await _actor.Deactivate()
                    .ConfigureAwait(false);
            }
            finally
            {
                _actor = null;
                UnsubscribeDeactivationWatchdog();
            }
    }
}
