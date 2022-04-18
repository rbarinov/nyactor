namespace NYActor.Patterns.Reminder.Events;

public class SubscribedEvent : ReminderEvent
{
    public string SubscriptionKey { get; }
    public DateTime StartAt { get; }
    public TimeSpan? Period { get; }

    public SubscribedEvent(
        string key,
        DateTime eventAt,
        string subscriptionKey,
        DateTime startAt,
        TimeSpan? period
    )
        : base(key, eventAt)
    {
        SubscriptionKey = subscriptionKey;
        StartAt = startAt;
        Period = period;
    }
}
