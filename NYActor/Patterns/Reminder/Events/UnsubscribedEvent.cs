namespace NYActor.Patterns.Reminder.Events;

public class UnsubscribedEvent : ReminderEvent
{
    public string SubscriptionKey { get; }

    public UnsubscribedEvent(
        string key,
        DateTime eventAt,
        string subscriptionKey
    )
        : base(key, eventAt)
    {
        SubscriptionKey = subscriptionKey;
    }
}
