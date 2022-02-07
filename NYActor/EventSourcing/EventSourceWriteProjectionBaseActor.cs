using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NYActor.EventSourcing;

public abstract class EventSourceWriteProjectionBaseActor : Actor
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;
    private readonly IEventSourceWriteProjectionPositionProvider _eventSourceWriteProjectionPositionProvider;
    private readonly Subject<EventSourceEventContainer> _eventSubject = new();

    private readonly Subject<Unit> _unsubscribeAll = new();

    protected EventSourceWriteProjectionBaseActor(
        IEventSourcePersistenceProvider eventSourcePersistenceProvider,
        IEventSourceWriteProjectionPositionProvider eventSourceWriteProjectionPositionProvider
    )
    {
        _eventSourcePersistenceProvider = eventSourcePersistenceProvider;
        _eventSourceWriteProjectionPositionProvider = eventSourceWriteProjectionPositionProvider;

        ObserveEvents(_eventSubject);
    }

    protected override async Task OnActivated()
    {
        await base.OnActivated();

        this.EnableDeactivationDelay(_unsubscribeAll);

        var syncPosition = await _eventSourceWriteProjectionPositionProvider.ReadPositionAsync(
            GetType()
        );

        _eventSourcePersistenceProvider.ObserveAllEvents(syncPosition.SyncPosition)
            .TakeUntil(_unsubscribeAll)
            .Subscribe(_eventSubject);
    }

    protected async Task WriteProjectionPositionAsync(string syncPosition)
    {
        await _eventSourceWriteProjectionPositionProvider.WritePositionAsync(
            GetType(),
            syncPosition
        );
    }

    protected virtual void ObserveEvents(IObservable<EventSourceEventContainer> eventObservable)
    {
    }

    protected override async Task OnDeactivated()
    {
        _unsubscribeAll?.OnNext(Unit.Default);
        _unsubscribeAll?.OnCompleted();

        await base.OnDeactivated();
    }
}
