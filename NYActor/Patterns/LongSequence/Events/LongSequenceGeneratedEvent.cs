namespace NYActor.Patterns.LongSequence.Events;

public class LongSequenceGeneratedEvent
{
    public DateTime EventAt { get; }
    public long Value { get; }

    public LongSequenceGeneratedEvent(DateTime eventAt, long value)
    {
        EventAt = eventAt;
        Value = value;
    }
}
