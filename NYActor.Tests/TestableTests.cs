using System;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NYActor.EventStore;
using NYActor.EventStore.Testable;

namespace NYActor.Tests
{
    public class TestableTests
    {
        private IEventStoreConnection _conn;
        private Random _random;
        private TestableActorFactory _factory;

        [SetUp]
        public void SetUp()
        {
            var userCredentials = new UserCredentials("admin", "changeit");

            var connectionSettingsBuilder = ConnectionSettings.Create()
                .SetDefaultUserCredentials(userCredentials)
                .Build();

            var es = EventStoreConnection.Create(
                connectionSettingsBuilder,
                new IPEndPoint(IPAddress.Any, 1113)
            );

            es.ConnectAsync()
                .Wait();

            _conn = es;

            _random = new Random();

            _factory = new TestableActorFactoryBuilder()
                .WithEventStoreConnection(_conn)
                .ConfigureServices(
                    e => { e.RegisterSingleton<ITimeService>(new NativeTimeService()); }
                )
                .Build();
        }

        [TearDown]
        public void TearDown()
        {
            _conn.Dispose();
        }

        [Test]
        public async Task TestActorStateMutationWithCommand()
        {
            long userId = _random.Next(0, int.MaxValue);

            var userFacade = await _factory.GetActorFacade<UserActor>(userId.ToString());

            var info1 = await userFacade.InvokeAsync(e => e.GetInfo());

            Assert.True(string.IsNullOrWhiteSpace(info1.Username));

            await userFacade.InvokeAsync(e => e.Signup("username_with_command"));
            var info2 = await userFacade.InvokeAsync(e => e.GetInfo());

            Assert.AreEqual("username_with_command", info2.Username);

            await userFacade.RefreshState();

            // totally reload actor
            var info3 = await userFacade.InvokeAsync(e => e.GetInfo());

            Assert.AreEqual("username_with_command", info3.Username);

            Assert.Pass();
        }

        [Test]
        public async Task TestActorStateMutationWithEventInjection()
        {
            long userId = _random.Next(0, int.MaxValue);

            var userFacade = await _factory.GetActorFacade<UserActor>(userId.ToString());

            var info1 = await userFacade.InvokeAsync(e => e.GetInfo());

            Assert.True(string.IsNullOrWhiteSpace(info1.Username));

            await userFacade.ApplySingleAsync(
                new UserSignupEvent(userId, DateTime.UtcNow, "username_with_event_injection")
            );

            var info2 = await userFacade.InvokeAsync(e => e.GetInfo());

            Assert.AreEqual("username_with_event_injection", info2.Username);

            await userFacade.RefreshState();

            // totally reload actor
            var info3 = await userFacade.InvokeAsync(e => e.GetInfo());

            Assert.AreEqual("username_with_event_injection", info3.Username);

            Assert.Pass();
        }
    }

    #region External Code

    #region DI

    public interface ITimeService
    {
        DateTime UtcNow { get; }
    }

    public class NativeTimeService : ITimeService
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    #endregion

    #region Actors

    public abstract class UserEventBase
    {
        public long UserId { get; }
        public DateTime EventAt { get; }

        public UserEventBase(long userId, DateTime eventAt)
        {
            UserId = userId;
            EventAt = eventAt;
        }
    }

    public sealed class UserSignupEvent : UserEventBase
    {
        public string Username { get; }

        public UserSignupEvent(
            long userId,
            DateTime eventAt,
            string username
        )
            : base(userId, eventAt)
        {
            Username = username;
        }
    }

    public class UserState : IApplicable
    {
        public void Apply(object ev)
        {
            if (ev is UserSignupEvent userSignupEvent)
            {
                Username = userSignupEvent.Username;
                SignupAt = userSignupEvent.EventAt;
            }
        }

        public DateTime SignupAt { get; private set; }

        public string Username { get; private set; }
    }

    public sealed class UserActor : EventStorePersistedActor<UserState>
    {
        private readonly ITimeService _timeService;

        public UserActor(IEventStoreConnection eventStoreConnection, ITimeService timeService)
            : base(eventStoreConnection)
        {
            _timeService = timeService;
        }

        private long UserId => long.Parse(Key);

        public async Task Signup(string username)
        {
            var ev = new UserSignupEvent(
                UserId,
                _timeService.UtcNow,
                username
            );

            await ApplySingleAsync(ev);
        }

        public Task<UserInfo> GetInfo() =>
            Task.FromResult(
                new UserInfo(
                    UserId,
                    State.SignupAt,
                    State.Username
                )
            );
    }

    public class UserInfo
    {
        public long UserId { get; }
        public DateTime SignupAt { get; }
        public string Username { get; }

        public UserInfo(
            long userId,
            DateTime signupAt,
            string username
        )
        {
            UserId = userId;
            SignupAt = signupAt;
            Username = username;
        }
    }

    #endregion

    #endregion
}
