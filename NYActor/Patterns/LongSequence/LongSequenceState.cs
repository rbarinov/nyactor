using NYActor.EventSourcing;
using NYActor.Patterns.LongSequence.Events;

namespace NYActor.Patterns.LongSequence;

public class LongSequenceState : IApplicable
{
    public long Value { get; private set; }

    public void Apply(object ev)
    {
        if (ev is LongSequenceGeneratedEvent generatedEvent)
        {
            Value = generatedEvent.Value;
        }
    }
}
