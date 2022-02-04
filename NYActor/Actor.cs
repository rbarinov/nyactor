namespace NYActor;

public abstract class Actor : IActor
{
    public string Key { get; private set; }

    internal IActorDispatcherInternal SelfDispatcherInternal { get; private set; }

    internal void InitializeInstanceFields(
        string key,
        IActorDispatcherInternal selfDispatcherInternal
    )
    {
        Key = key;
        SelfDispatcherInternal = selfDispatcherInternal;
    }

    internal async Task Activate()
    {
        await OnActivated()
            .ConfigureAwait(false);
    }

    internal async Task Deactivate()
    {
        await OnDeactivated()
            .ConfigureAwait(false);
    }

    protected virtual Task OnActivated()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDeactivated()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnMessage(object message)
    {
        return Task.CompletedTask;
    }

    protected void DelayDeactivation(TimeSpan deactivationTimeout)
    {
        SelfDispatcherInternal.DelayDeactivation(deactivationTimeout);
    }
}
