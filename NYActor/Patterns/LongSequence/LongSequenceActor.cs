using NYActor.EventSourcing;
using NYActor.Patterns.LongSequence.Events;

namespace NYActor.Patterns.LongSequence;

public class LongSequenceActor : EventSourcePersistedActor<LongSequenceState>
{
    private readonly ITimeProvider _timeProvider;

    public LongSequenceActor(IEventSourcePersistenceProvider eventSourcePersistenceProvider, ITimeProvider timeProvider)
        : base(eventSourcePersistenceProvider)
    {
        _timeProvider = timeProvider;
    }

    public async Task<long> Generate()
    {
        var ev = new LongSequenceGeneratedEvent(_timeProvider.UtcNow, State.Value + 1);
        await ApplySingleAsync(ev);

        return State.Value;
    }

    public Task<long> GetLastId() =>
        Task.FromResult(State.Value);
}
