namespace NYActor.EventSourcing;

public class EventSourceSubscriptionCatchUp
{
    public EventSourceSubscriptionCatchUp(bool isSubscribedToAll, string streamId, string subscriptionName)
    {
        IsSubscribedToAll = isSubscribedToAll;
        StreamId = streamId;
        SubscriptionName = subscriptionName;
    }

    public bool IsSubscribedToAll { get; }
    public string StreamId { get; }
    public string SubscriptionName { get; }
}