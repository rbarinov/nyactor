using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using SimpleInjector;

namespace NYActor.Core
{
    public class GenericActorWrapper<TActor> : IActorWrapper<TActor>, IDisposable where TActor : Actor
    {
        private readonly string _key;
        private readonly Node _node;
        private readonly Container _container;
        private TActor _actor;

        private readonly Subject<ActorMessage> _ingressSubject = new Subject<ActorMessage>();
        private readonly IDisposable _ingressSubscription;

        private Subject<Unit> _deactivationWatchdogSubject = null;
        private IDisposable _deactivationWatchdogSubscription = null;

        public GenericActorWrapper(string key, Node node, Container container)
        {
            _key = key;
            _node = node;
            _container = container;

            _ingressSubscription = _ingressSubject
                .Select(e => Observable
                    .Defer(() => Observable
                        .StartAsync(() => Task.Factory.StartNew(() => HandleIngressMessage(e)).Unwrap())
                    )
                )
                .Merge(1)
                .Subscribe();
        }

        private async Task HandleIngressMessage(ActorMessage actorMessage)
        {
            if (actorMessage is PoisonPill)
            {
                if (_actor != null)
                {
                    try
                    {
                        await DeactivateActorInstance().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // @todo handle deactivation unhandled exception
                    }
                }

                return;
            }

            if (_actor == null)
            {
                try
                {
                    await ActivateActorInstance().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // @todo handle activation unhandled exception
                }
            }

            ThrottleDeactivation();

            switch (actorMessage)
            {
                case IngressActorMessage ingressActorMessage:
                    try
                    {
                        await _actor.OnMessage(ingressActorMessage.Payload);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            await DeactivateActorInstance().ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            // @todo handle deactivation unhandled exception
                        }
                    }

                    break;
                case IngressAskMessage ingressAskMessage:
                    try
                    {
                        var response = await ingressAskMessage.Invoke(_actor).ConfigureAwait(false);
                        ingressAskMessage.TaskCompletionSource.TrySetResult(response);
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await DeactivateActorInstance().ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            // @todo handle deactivation unhandled exception
                        }

                        ingressAskMessage.TaskCompletionSource.TrySetException(ex);
                    }

                    break;
            }

            ThrottleDeactivation();
        }

        private async Task ActivateActorInstance()
        {
            _actor = _container.GetInstance<TActor>();
            _actor.Key = _key;
            _actor.Context = new ActorContext(this, _node);

            SubscribeDeactivationWatchdog();

            await _actor.Activate().ConfigureAwait(false);
        }

        private void ThrottleDeactivation() => _deactivationWatchdogSubject.OnNext(Unit.Default);

        private void SubscribeDeactivationWatchdog(TimeSpan? deactivationTimeout = null)
        {
            UnsubscribeDeactivationWatchdog();

            _deactivationWatchdogSubject = new Subject<Unit>();

            _deactivationWatchdogSubscription = _deactivationWatchdogSubject
                .Timeout(deactivationTimeout ?? _node.DefaultActorDeactivationTimeout)
                .IgnoreElements()
                .Subscribe(_ => { }, timeout => _ingressSubject.OnNext(PoisonPill.Default));
        }

        private void UnsubscribeDeactivationWatchdog()
        {
            _deactivationWatchdogSubscription?.Dispose();
            _deactivationWatchdogSubscription = null;

            _deactivationWatchdogSubject?.Dispose();
            _deactivationWatchdogSubject = null;
        }

        private async Task DeactivateActorInstance()
        {
            if (_actor != null)
            {
                try
                {
                    await _actor.Deactivate().ConfigureAwait(false);
                }
                finally
                {
                    _actor = null;
                    UnsubscribeDeactivationWatchdog();
                }
            }
        }

        public void Dispose()
        {
            DeactivateActorInstance().Wait();
            _ingressSubscription.Dispose();
            _ingressSubject.Dispose();
        }

        public void DelayDeactivation(TimeSpan deactivationTimeout) =>
            SubscribeDeactivationWatchdog(deactivationTimeout);

        public Task SendAsync<TMessage>(TMessage message)
        {
            var messageWrapper = new IngressActorMessage(message);
            _ingressSubject.OnNext(messageWrapper);
            return Task.CompletedTask;
        }

        public async Task<TResult> InvokeAsync<TResult>(Func<TActor, Task<TResult>> req)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            _ingressSubject.OnNext(new IngressAskMessage(
                async e => (object) await req((TActor) e),
                taskCompletionSource
            ));

            var response = await taskCompletionSource.Task.ConfigureAwait(false);

            return (TResult) response;
        }

        public async Task InvokeAsync(Func<TActor, Task> req)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            _ingressSubject.OnNext(new IngressAskMessage(
                async e =>
                {
                    await req((TActor) e);
                    return Unit.Default;
                },
                taskCompletionSource
            ));

            await taskCompletionSource.Task.ConfigureAwait(false);
        }
    }
}