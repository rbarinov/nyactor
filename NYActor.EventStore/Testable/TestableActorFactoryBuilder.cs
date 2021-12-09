using System;
using EventStore.ClientAPI;
using SimpleInjector;

namespace NYActor.EventStore.Testable
{
    public class TestableActorFactoryBuilder
    {
        private IEventStoreConnection _eventStoreConnection;
        private Action<Container> _configureServices;

        public TestableActorFactoryBuilder WithEventStoreConnection(IEventStoreConnection eventStoreConnection)
        {
            _eventStoreConnection = eventStoreConnection;

            return this;
        }

        public TestableActorFactoryBuilder ConfigureServices(Action<Container> configureServices)
        {
            _configureServices = configureServices;

            return this;
        }

        public TestableActorFactory Build()
        {
            return new TestableActorFactory(
                _eventStoreConnection,
                _configureServices
            );
        }
    }
}
