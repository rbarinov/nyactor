using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Newtonsoft.Json;

namespace NYActor.EventSourcing;

public abstract class EventSourceWriteProjectionBaseActor : Actor
{
    private readonly IEventSourcePersistenceProvider _eventSourcePersistenceProvider;
    private readonly IEventSourceWriteProjectionPositionProvider _eventSourceWriteProjectionPositionProvider;
    private readonly Subject<EventSourceEvent> _eventSubject = new();

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
            .Select(e => new EventSourceEvent(e.Position, e.EventData.EventType, DeserializeEvent(e)))
            .Subscribe(_eventSubject);
    }

    protected virtual object DeserializeEvent(EventSourceEventContainer eventContainer)
    {
        var json = Encoding.UTF8.GetString(eventContainer.EventData.Event);

        var type = Type.GetType(eventContainer.EventData.EventType);

        if (type == null)
        {
            return null;
        }

        var @event = JsonConvert.DeserializeObject(json, type);

        return @event;
    }

    protected async Task WriteProjectionPositionAsync(string syncPosition)
    {
        await _eventSourceWriteProjectionPositionProvider.WritePositionAsync(
            GetType(),
            syncPosition
        );
    }

    protected virtual void ObserveEvents(IObservable<EventSourceEvent> eventObservable)
    {
    }

    protected override async Task OnDeactivated()
    {
        _unsubscribeAll?.OnNext(Unit.Default);
        _unsubscribeAll?.OnCompleted();

        await base.OnDeactivated();
    }
}
