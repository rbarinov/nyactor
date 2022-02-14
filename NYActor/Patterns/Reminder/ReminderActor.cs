using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NYActor.EventSourcing;
using NYActor.Patterns.Reminder.Events;

namespace NYActor.Patterns.Reminder;

public abstract class ReminderActor : EventSourcePersistedActor<ReminderActorState>
{
    private readonly ITimeProvider _timeProvider;

    private Subject<Unit> _unSubscribeAll = new();

    protected ReminderActor(
        IEventStoreV5EventSourcePersistenceProvider eventSourcePersistenceProvider,
        ITimeProvider timeProvider
    )
        : base(eventSourcePersistenceProvider)
    {
        _timeProvider = timeProvider;
    }

    protected abstract Task Remind(string key, ActorExecutionContext executionContext);

    public Task WakeUp()
    {
        return Task.CompletedTask;
    }

    protected override async Task OnActivated()
    {
        await base.OnActivated();

        this.EnableDeactivationDelay(_unSubscribeAll);

        Observable
            .FromAsync(
                async () => await this.Self()
                    .InvokeAsync(s => s.GetSubscriptions(), ActorExecutionContext.Empty)
            )
            .SelectMany(
                e =>
                    e.subscriptions.Select(
                        s =>
                            Observable.FromAsync(
                                async () =>
                                {
                                    try
                                    {
                                        await Remind(s.Key, e.context);

                                        if (!s.Period.HasValue)
                                        {
                                            await this.Self()
                                                .InvokeAsync(u => u.Unsubscribe(s.Key), e.context);
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        Console.WriteLine(exception);
                                    }

                                    await this.Self()
                                        .InvokeAsync(ss => ss.SetSubscriptionLastStartAt(s), e.context);
                                }
                            )
                    )
            )
            .Merge()
            .RepeatAfterDelay(TimeSpan.FromSeconds(5))
            .TakeUntil(_unSubscribeAll)
            .Subscribe();
    }

    protected override async Task OnDeactivated()
    {
        _unSubscribeAll?.OnNext(Unit.Default);
        _unSubscribeAll?.OnCompleted();

        await base.OnDeactivated();
    }

    private Task SetSubscriptionLastStartAt(Subscription subscription)
    {
        subscription.LastStartAt = _timeProvider.UtcNow;

        return Task.CompletedTask;
    }

    private Task<(List<Subscription> subscriptions, ActorExecutionContext context)> GetSubscriptions()
    {
        return Task.FromResult(
            (subscriptions: State.Subscriptions
                    .Where(
                        e => e.Value.StartAt < _timeProvider.UtcNow &&
                             (!e.Value.LastStartAt.HasValue || (e.Value.LastStartAt.Value +
                                                                (e.Value.Period ?? TimeSpan.Zero)) <
                                 _timeProvider.UtcNow)
                    )
                    .Select(e => e.Value)
                    .ToList(),
                context: this.ActorExecutionContext())
        );
    }

    public async Task Subscribe(string key, TimeSpan dueTime, TimeSpan? period)
    {
        if (!State.Subscriptions.ContainsKey(key))
        {
            var subscribedEvent = new SubscribedEvent(
                Key,
                _timeProvider.UtcNow,
                key,
                _timeProvider.UtcNow + dueTime,
                period
            );

            await ApplySingleAsync(subscribedEvent);
        }
    }

    public async Task Unsubscribe(string key)
    {
        if (State.Subscriptions.ContainsKey(key))
        {
            var unsubscribedEvent = new UnsubscribedEvent(
                Key,
                _timeProvider.UtcNow,
                key
            );

            await ApplySingleAsync(unsubscribedEvent);
        }
    }
}
