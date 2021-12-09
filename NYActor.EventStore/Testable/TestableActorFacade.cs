using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NYActor.Core;
using SimpleInjector;

namespace NYActor.EventStore.Testable
{
    public class TestableActorFacade<TActor>
        where TActor : Actor, ITestableEventStorePersistedActor
    {
        private readonly string _key;
        private readonly Container _container;
        private TActor _actor;

        internal TestableActorFacade(string key, Container container)
        {
            _key = key;
            _container = container;
        }

        public async Task RefreshState()
        {
            _actor = null;

            var actor = _container.GetInstance<TActor>();
            actor.Key = _key;
            actor.Context = new ActorContext(default, default);

            await actor.Activate();

            _actor = actor;
        }

        public Task<TResult> InvokeAsync<TResult>(Func<TActor, Task<TResult>> @delegate) =>
            @delegate(_actor);

        public Task InvokeAsync(Func<TActor, Task> @delegate) =>
            @delegate(_actor);

        public Task ApplyMultipleAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : class
        {
            var persistedActor = (ITestableEventStorePersistedActor) _actor;

            return persistedActor.UnsafeApplyMultipleAsync(events);
        }

        public Task ApplySingleAsync<TEvent>(TEvent @event) where TEvent : class =>
            ApplyMultipleAsync(Enumerable.Repeat(@event, 1));
    }
}
