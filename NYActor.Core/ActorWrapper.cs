using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using SimpleInjector;

namespace NYActor.Core
{
    public class ActorWrapper<TActor> : ActorWrapperBase where TActor : Actor
    {
        private readonly string _key;
        private readonly Node _node;
        private readonly Container _container;
        private TActor _actor;

        internal ActorWrapper(string actorPath, string key, Node node, Container container) : base(actorPath)
        {
            _key = key;
            _node = node;
            _container = container;
        }

        internal override async Task OnMessageEnqueued(MessageQueueItem messageQueueItem)
        {
            try
            {
                if (messageQueueItem == PoisonPill.Default && _actor != null)
                {
                    await DeactivateActorInstance().ConfigureAwait(false);
                    return;
                }

                if (_actor == null)
                {
                    await ActivateActorInstance().ConfigureAwait(false);
                }

                var res = await messageQueueItem.Req(_actor).ConfigureAwait(false);

                messageQueueItem.Tsc.SetResult(res);

                ThrottleDeactivation(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await DeactivateActorInstance().ConfigureAwait(false);
                messageQueueItem.Tsc.SetException(ex);
            }
        }

        private DateTime _lastCall = default;
        private CancellationTokenSource _cancellationTokenSource;

        public void DelayDeactivation(TimeSpan deactivationBlock) =>
            ThrottleDeactivation(DateTime.UtcNow.Add(deactivationBlock), deactivationBlock);

        private void ThrottleDeactivation(DateTime utcNow, TimeSpan? deactivationBlock = null)
        {
            if (utcNow.Subtract(_lastCall + _node.DefaultActorDeactivationThrottleInterval) <
                _node.DefaultActorDeactivationThrottleInterval) return;

            if (_lastCall >= utcNow) return;

            _lastCall = utcNow;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            Task.Delay(deactivationBlock ?? _node.DefaultActorDeactivationInterval, cancellationToken)
                .ContinueWith(e =>
                {
                    if (e.IsCanceled) return;
                    _node.MessageQueueObserver.OnNext((ActorPath, PoisonPill.Default));
                }, cancellationToken);
        }

        private async Task ActivateActorInstance()
        {
            _actor = _container.GetInstance<TActor>();
            _actor.Key = _key;
            _actor.Context = new ActorContext(this, _node);

            await _actor.Activate().ConfigureAwait(false);
        }

        private async Task DeactivateActorInstance()
        {
            if (_actor != null)
            {
                await _actor.Deactivate().ConfigureAwait(false);
                _actor = null;
            }
        }

        public async Task<TResult> InvokeAsync<TResult>(Func<TActor, Task<TResult>> req)
        {
            var tcs = new TaskCompletionSource<object>();

            _node.MessageQueueObserver.OnNext((ActorPath, new MessageQueueItem
            {
                Req = async e => (object) await req((TActor) e),
                Tsc = tcs
            }));

            var res = await tcs.Task.ConfigureAwait(false);

            return (TResult) res;
        }

        public async Task InvokeAsync(Func<TActor, Task> req)
        {
            var tcs = new TaskCompletionSource<object>();

            _node.MessageQueueObserver.OnNext((ActorPath, new MessageQueueItem
            {
                Req = async e =>
                {
                    await req((TActor) e).ConfigureAwait(false);
                    return Unit.Default;
                },
                Tsc = tcs
            }));

            await tcs.Task.ConfigureAwait(false);
        }

        public override void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}