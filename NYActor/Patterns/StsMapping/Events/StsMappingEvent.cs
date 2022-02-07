namespace NYActor.Patterns.StsMapping.Events;

public class StsMappingEvent
{
    public string Key { get; }
    public DateTime EventAt { get; }

    public StsMappingEvent(
        string key,
        DateTime eventAt
    )
    {
        Key = key;
        EventAt = eventAt;
    }
}
