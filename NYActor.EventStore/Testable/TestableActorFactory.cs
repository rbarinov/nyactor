using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using NYActor.Core;
using SimpleInjector;

namespace NYActor.EventStore.Testable
{
    public class TestableActorFactory
    {
        private readonly Action<Container> _configureServices;
        private readonly Container _container;
        private readonly HashSet<Type> _registeredActorTypes = new HashSet<Type>();

        internal TestableActorFactory(IEventStoreConnection eventStoreConnection, Action<Container> configureServices)
        {
            _configureServices = configureServices;

            _container = new Container();

            _container.RegisterInstance(eventStoreConnection);
            configureServices(_container);
        }

        public async Task<TestableActorFacade<TActor>> GetActorFacade<TActor>(string key)
            where TActor : Actor, ITestableEventStorePersistedActor
        {
            var tActor = typeof(TActor);

            if (!_registeredActorTypes.Contains(tActor))
            {
                _container.Register(tActor);
                _registeredActorTypes.Add(tActor);
            }

            var facade = new TestableActorFacade<TActor>(key, _container);

            await facade.RefreshState();

            return facade;
        }
    }
}
