using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
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


        protected Task ApplySingleAsync<TEvent>(TEvent @event) where TEvent : class =>
            ApplyMultipleAsync(Enumerable.Repeat(@event, 1));

        protected async Task ApplyMultipleAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : class
        {
            var materializedEvents = events.ToList();

            if (!materializedEvents.Any()) return;

            var esEvents = materializedEvents
                .Select(e => new EventData(
                    Guid.NewGuid(),
                    $"{e.GetType().FullName},{e.GetType().Assembly.GetName().Name}",
                    true,
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e)),
                    null
                ));

            try
            {
                await _eventStoreConnection.AppendToStreamAsync(Stream, _version, esEvents).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException e)
            {
                if (e.ActualVersion != _version + 1)
                {
                    throw;
                }
            }

            foreach (var @event in materializedEvents)
            {
                State.Apply(@event);
                _version++;
            }
        }

        protected virtual int ActivationEventReadBatchSize => 4096;

        protected virtual object DeserializeEvevnt(string typeName, string json)
        {
            var type = Type.GetType(typeName);
            var @event = JsonConvert.DeserializeObject(json, type);

            return @event;
        }
        
        protected override async Task OnActivated()
        {
            await base.OnActivated().ConfigureAwait(false);

            await Observable.Create<ResolvedEvent>(async observer =>
                {
                    var read = 0;

                    try
                    {
                        do
                        {
                            var batch = await _eventStoreConnection.ReadStreamEventsForwardAsync(
                                Stream,
                                read,
                                ActivationEventReadBatchSize,
                                false
                            );

                            foreach (var @event in batch.Events)
                            {
                                observer.OnNext(@event);
                            }

                            read += batch.Events.Length;

                            if (batch.IsEndOfStream) break;
                        } while (true);

                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                })
                .Do(e =>
                {
                    var json = Encoding.UTF8.GetString(e.Event.Data);
                    var typeName = e.Event.EventType;
                    var @event = DeserializeEvevnt(typeName, json);
                    var version = e.Event.EventNumber;

                    State.Apply(@event);
                    _version = version;
                })
                .IgnoreElements()
                .DefaultIfEmpty()
                .ToTask()
                .ConfigureAwait(false);
        }
    }
}