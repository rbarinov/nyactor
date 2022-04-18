using NYActor.EventSourcing;
using NYActor.Patterns.StsMapping.Events;

namespace NYActor.Patterns.StsMapping;

public class StsMappingState : IApplicable
{
    public string AttachedKey { get; set; }
    public DateTime? AttachedAt { get; set; }

    public void Apply(object ev)
    {
        if (ev is StsMappingAttachedEvent attachedEvent)
        {
            AttachedKey = attachedEvent.AttachedKey;
            AttachedAt = attachedEvent.EventAt;
        }
        else if (ev is StsMappingDetachedEvent detachedEvent)
        {
            AttachedKey = null;
            AttachedAt = detachedEvent.EventAt;
        }
    }
}
