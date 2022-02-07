namespace NYActor.Patterns.StsMapping.Events;

public class StsMappingAttachedEvent : StsMappingEvent
{
    public string AttachedKey { get; }

    public StsMappingAttachedEvent(
        string key,
        DateTime eventAt,
        string attachedKey
    )
        : base(key, eventAt)
    {
        AttachedKey = attachedKey;
    }
}
