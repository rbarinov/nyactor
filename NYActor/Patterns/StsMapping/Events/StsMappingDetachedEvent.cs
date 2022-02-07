namespace NYActor.Patterns.StsMapping.Events;

public class StsMappingDetachedEvent : StsMappingEvent
{
    public StsMappingDetachedEvent(string key, DateTime eventAt)
        : base(key, eventAt)
    {
    }
}
