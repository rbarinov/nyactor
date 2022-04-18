namespace NYActor.Patterns.Reminder.Events;

public class ReminderEvent
{
    public string Key { get; }
    public DateTime EventAt { get; }

    public ReminderEvent(
        string key,
        DateTime eventAt
    )
    {
        Key = key;
        EventAt = eventAt;
    }
}
