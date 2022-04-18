namespace NYActor.Patterns.StsMapping;

public class StsMappingInfo
{
    public string AttachedKey { get; }
    public DateTime? AttachedAt { get; }

    public StsMappingInfo(string attachedKey, DateTime? attachedAt)
    {
        AttachedKey = attachedKey;
        AttachedAt = attachedAt;
    }
}