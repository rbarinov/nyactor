using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NYActor.Core.Extensions;
using NYActor.Core.RequestPropagation;
using OpenTelemetry.Trace;
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
        private readonly string _fullName;

        public GenericActorWrapper(string key, Node node, Container container)
        {
            _key = key;
            _node = node;
            _container = container;

            _fullName = $"{typeof(TActor).FullName}-{_key}";

            _ingressSubscription = _ingressSubject
                .Select(
                    e => Observable
                        .Defer(
                            () => Observable
                                .StartAsync(
                                    () => Task.Factory.StartNew(() => HandleIngressMessage(e))
                                        .Unwrap()
                                )
                        )
                )
                .Merge(1)
                .Subscribe();
        }

        internal ActorExecutionContext ExecutionContext { get; private set; }
        internal Activity Activity { get; private set; }

        private Activity CreateActivity(ActorExecutionContext executionContext, string callName)
        {
            if (_node.TracingEnabled)
            {
                var activitySource = _container.GetInstance<ActivitySource>();
                Activity.Current = null;
                ExecutionContext = executionContext;
                var context = ExecutionContext?.To<RequestPropagationExecutionContext>();

                var activityContext = context?.RequestPropagationValues != null &&
                                      context.RequestPropagationValues.ContainsKey("x-b3-traceid") &&
                                      context.RequestPropagationValues.ContainsKey("x-b3-spanid")
                    ? (ActivityContext?)new ActivityContext(
                        ActivityTraceId.CreateFromString(context.RequestPropagationValues["x-b3-traceid"]),
                        ActivitySpanId.CreateFromString(context.RequestPropagationValues["x-b3-spanid"]),
                        ActivityTraceFlags.Recorded
                    )
                    : null;

                var headers = ExecutionContext?.To<RequestPropagationExecutionContext>()
                    ?.RequestPropagationValues;

                var activity = activityContext != null
                    ? activitySource.StartActivity(
                        $"{_fullName}: {callName}",
                        ActivityKind.Server,
                        activityContext.Value
                    )
                    : activitySource.StartActivity(
                        $"{_fullName}: {callName}",
                        ActivityKind.Server
                    );

                if (activity != null)
                {
                    if (headers != null)
                    {
                        var updated = new Dictionary<string, string>(headers)
                        {
                            ["x-b3-traceid"] = activity.TraceId.ToString(),
                            ["x-b3-spanid"] = activity.SpanId.ToString()
                        };

                        ExecutionContext = new RequestPropagationExecutionContext(updated);
                    }
                    else
                    {
                        var updated = new Dictionary<string, string>()
                        {
                            ["x-request-id"] = Guid.NewGuid()
                                .ToString(),
                            ["x-b3-traceid"] = activity.TraceId.ToString(),
                            ["x-b3-spanid"] = activity.SpanId.ToString(),
                            ["x-b3-sampled"] = "1"
                            // { "x-b3-parentspanid", "" },
                            // { "x-b3-flags", "" },
                            // { "x-ot-span-context", "" },
                        };

                        ExecutionContext = new RequestPropagationExecutionContext(updated);
                    }
                }

                return activity;
            }

            return null;
        }

        private async Task HandleIngressMessage(ActorMessage actorMessage)
        {
            ExecutionContext = null;

            //todo
            if (actorMessage is PoisonPill || (actorMessage as IngressActorMessage)?.Payload is PoisonPill)
            {
                if (_actor != null)
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

                return;
            }

            if (_actor == null)
            {
                try
                {
                    await ActivateActorInstance()
                        .ConfigureAwait(false);
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
                            await DeactivateActorInstance()
                                .ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            // @todo handle deactivation unhandled exception
                        }
                    }

                    break;

                case IngressAskMessage ingressAskMessage:
                {
                    Activity = CreateActivity(ingressAskMessage.ExecutionContext, ingressAskMessage.CallName);

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

                        Activity?.RecordException(ex);
                        Activity?.SetStatus(Status.Error.WithDescription(ex.Message));
                    }
                    finally
                    {
                        Activity?.Dispose();
                        ExecutionContext = null;
                    }

                    break;
                }
            }

            ThrottleDeactivation();
        }

        private async Task ActivateActorInstance()
        {
            _actor = _container.GetInstance<TActor>();
            _actor.Key = _key;
            _actor.Context = new ActorContext(this, _node);

            SubscribeDeactivationWatchdog();

            await _actor.Activate()
                .ConfigureAwait(false);
        }

        private void ThrottleDeactivation() =>
            _deactivationWatchdogSubject?.OnNext(Unit.Default);

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

        public void Dispose()
        {
            DeactivateActorInstance()
                .Wait();

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

        public async Task<TResult> InvokeAsync<TResult>(
            Func<TActor, Task<TResult>> req,
            string callName,
            ActorExecutionContext executionContext
        )
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            _ingressSubject.OnNext(
                new IngressAskMessage(
                    async e => (object)await req((TActor)e),
                    taskCompletionSource,
                    callName,
                    executionContext
                )
            );

            var response = await taskCompletionSource.Task.ConfigureAwait(false);

            return (TResult)response;
        }

        public async Task InvokeAsync(Func<TActor, Task> req, string callName, ActorExecutionContext executionContext)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            _ingressSubject.OnNext(
                new IngressAskMessage(
                    async e =>
                    {
                        await req((TActor)e);

                        return Unit.Default;
                    },
                    taskCompletionSource,
                    callName,
                    executionContext
                )
            );

            await taskCompletionSource.Task.ConfigureAwait(false);
        }
    }
}