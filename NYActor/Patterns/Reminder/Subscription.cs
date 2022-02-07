namespace NYActor.Patterns.Reminder;

public class Subscription
{
    public string Key { get; }
    public DateTime StartAt { get; }
    public DateTime? LastStartAt { get; set; }
    public TimeSpan? Period { get; }

    public Subscription(
        string key,
        DateTime startAt,
        TimeSpan? period
    )
    {
        Key = key;
        StartAt = startAt;
        Period = period;
    }
}
