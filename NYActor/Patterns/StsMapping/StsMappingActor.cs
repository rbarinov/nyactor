using NYActor.EventSourcing;
using NYActor.Patterns.StsMapping.Events;

namespace NYActor.Patterns.StsMapping;

public class StsMappingActor : EventSourcePersistedActor<StsMappingState>
{
    private readonly ITimeProvider _timeProvider;

    public StsMappingActor(
        IEventStoreV5EventSourcePersistenceProvider eventSourcePersistenceProvider,
        ITimeProvider timeProvider
    )
        : base(eventSourcePersistenceProvider)
    {
        _timeProvider = timeProvider;
    }

    public async Task<StsMappingInfo> Attach(string attachedKey)
    {
        if (string.IsNullOrWhiteSpace(attachedKey)) return GetInfoFromState(State);

        var attachedEvent = new StsMappingAttachedEvent(
            Key,
            _timeProvider.UtcNow,
            attachedKey
        );

        await ApplySingleAsync(attachedEvent);

        return GetInfoFromState(State);
    }

    public async Task<StsMappingInfo> Detach()
    {
        var attachedEvent = new StsMappingDetachedEvent(
            Key,
            _timeProvider.UtcNow
        );

        await ApplySingleAsync(attachedEvent);

        return GetInfoFromState(State);
    }

    private static StsMappingInfo GetInfoFromState(StsMappingState state)
    {
        var info = new StsMappingInfo(
            state.AttachedKey,
            state.AttachedAt
        );

        return info;
    }

    public Task<StsMappingInfo> GetInfo() =>
        Task.FromResult(GetInfoFromState(State));
}
