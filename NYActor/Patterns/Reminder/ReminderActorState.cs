using NYActor.EventSourcing;
using NYActor.Patterns.Reminder.Events;

namespace NYActor.Patterns.Reminder;

public class ReminderActorState : IApplicable
{
    public readonly Dictionary<string, Subscription> Subscriptions = new();

    public void Apply(object ev)
    {
        switch (ev)
        {
            case SubscribedEvent subscribedEvent:
                Subscriptions.Add(
                    subscribedEvent.SubscriptionKey,
                    new Subscription(
                        subscribedEvent.SubscriptionKey,
                        subscribedEvent.StartAt,
                        subscribedEvent.Period
                    )
                );

                break;

            case UnsubscribedEvent unsubscribedEvent:
                Subscriptions.Remove(unsubscribedEvent.SubscriptionKey);

                break;
        }
    }
}
