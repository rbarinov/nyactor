namespace NYActor.EventSourcing;

public class EventSourceWriteProjectionState
{
    public string SyncPosition { get; }
    public DateTime? LastSyncAt { get; }

    public EventSourceWriteProjectionState(
        string syncPosition,
        DateTime? lastSyncAt
    )
    {
        SyncPosition = syncPosition;
        LastSyncAt = lastSyncAt;
    }
}
