using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json;
using NYActor.Core;

namespace NYActor.EventStore
{
    public abstract class EventStorePersistedActor<TState> : Actor
        where TState : class, IApplicable, new()
    {
        private readonly IEventStoreConnection _eventStoreConnection;

        protected EventStorePersistedActor(IEventStoreConnection eventStoreConnection)
        {
            State = new TState();

            _eventStoreConnection = eventStoreConnection;
            _version = -1;
        }

        protected TState State { get; }
        private long _version;
        protected long Version => _version;

        protected virtual string Stream => $"{GetType().FullName}-{Key}";


        protected Task ApplyEventAsync<TEvent>(TEvent @event) where TEvent : class => ApplyEventsAsync(@event);

        protected async Task ApplyEventsAsync<TEvent>(params TEvent[] events) where TEvent : class
        {
            if (!events.Any()) return;

            var esEvents = events
                .Select(e => new EventData(
                    Guid.NewGuid(),
                    $"{e.GetType().FullName},{e.GetType().Assembly.GetName().Name}",
                    true,
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e)),
                    null
                ));

            await _eventStoreConnection.AppendToStreamAsync(Stream, _version, esEvents).ConfigureAwait(false);

            foreach (var @event in events)
            {
                State.Apply(@event);
                _version++;
            }
        }

        protected override async Task OnActivated()
        {
            await base.OnActivated().ConfigureAwait(false);

            const int batchSize = 4096;

            var activationSubject = new Subject<ResolvedEvent>();

            var tcs = new TaskCompletionSource<Unit>();

            var activationTask = activationSubject
                .Select(e =>
                {
                    var json = Encoding.UTF8.GetString(e.Event.Data);
                    var typeName = e.Event.EventType;
                    var type = Type.GetType(typeName);
                    var @event = JsonConvert.DeserializeObject(json, type);
                    var version = e.Event.EventNumber;

                    State.Apply(@event);
                    _version = version;

                    return Unit.Default;
                })
                .Subscribe(e => { }, e => tcs.SetException(e), () => tcs.SetResult(Unit.Default));

            var read = 0;

            do
            {
                var batch = await _eventStoreConnection.ReadStreamEventsForwardAsync(
                    Stream,
                    read,
                    batchSize,
                    false
                );

                foreach (var @event in batch.Events)
                {
                    activationSubject.OnNext(@event);
                }

                read = batch.Events.Length;

                if (batch.IsEndOfStream) break;
            } while (true);

            activationSubject.OnCompleted();

            await tcs.Task.ConfigureAwait(false);

            activationSubject.Dispose();
        }
    }
}