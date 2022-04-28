using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NYActor.EventSourcing;
using NYActor.EventSourcing.S3;

namespace NYActor.Tests.V3;

public class S3EventStoreTests
{
    [Test]
    public async Task Test()
    {
        var s3AccessKeyId = Environment.GetEnvironmentVariable("S3AccessKeyId");
        var s3SecretAccessKey = Environment.GetEnvironmentVariable("S3SecretAccessKey");
        var s3EndpointUrl = Environment.GetEnvironmentVariable("S3EndpointUrl");
        var s3Space = Environment.GetEnvironmentVariable("S3Space");

        var client = new AmazonS3Client(
            s3AccessKeyId,
            s3SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL = s3EndpointUrl
            }
        );

        var directory = "nyactor-tests";

        var actorKey = Guid.NewGuid()
            .ToString();

        var actorSystem = new ActorSystemBuilder()
            .ConfigureServices(
                e =>
                {
                    e.AddSingleton<IS3EventSourcePersistenceProvider>(
                        new S3EventSourcePersistenceProvider(client, s3Space, directory)
                    );

                    e.AddSingleton<ITimeProvider>(new NaturalTimeProvider());
                }
            )
            .Build();

        var single = actorSystem.GetActor<S3PersistedActor>($"s3-test-single-{actorKey}");
        var multiple = actorSystem.GetActor<S3PersistedActor>($"s3-test-multiple-{actorKey}");

        var concurrency = 10;

        await Observable
            .Range(1, concurrency)
            .Select(
                e => Observable
                    .FromAsync(
                        () => single
                            .InvokeAsync(a => a.Set("message #" + e))
                    )
            )
            .Merge()
            .ToTask();

        var singleInfo = await single.InvokeAsync(e => e.GetInfo());

        Assert.AreEqual(concurrency, singleInfo.Count);

        var messagesCount = 100;

        await Observable
            .Range(1, concurrency)
            .Select(
                e => Observable
                    .FromAsync(
                        () => multiple
                            .InvokeAsync(a => a.Set(messagesCount, "message #" + e))
                    )
            )
            .Merge()
            .ToTask();

        var multipleInfo = await multiple.InvokeAsync(e => e.GetInfo());

        Assert.AreEqual(concurrency * messagesCount, multipleInfo.Count);
    }

    public class S3PersistedActor : EventSourcePersistedActor<S3PersistedState>
    {
        public S3PersistedActor(IS3EventSourcePersistenceProvider eventSourcePersistenceProvider)
            : base(eventSourcePersistenceProvider)
        {
        }

        public async Task Set(string message)
        {
            var ev = new S3Event(Key, DateTime.UtcNow, message);
            await ApplySingleAsync(ev);
        }

        public async Task Set(int count, string message)
        {
            var ev = Enumerable.Range(1, count)
                .Select(e => new S3Event(Key, DateTime.UtcNow, $"{message} #{e}"));

            await ApplyMultipleAsync(ev);
        }

        public Task<ReadOnlyCollection<string>> GetInfo()
        {
            return Task.FromResult(State.Messages.AsReadOnly());
        }
    }

    public class S3PersistedState : IApplicable
    {
        public List<string> Messages { get; private set; } = new List<string>();

        public void Apply(object ev)
        {
            switch (ev)
            {
                case S3Event s3Event:
                    Messages.Add(s3Event.Message);

                    break;
            }
        }
    }

    [DataContract]
    public class S3Event
    {
        [DataMember(Order = 0)]
        public string Key { get; set; }

        [DataMember(Order = 1)]
        public DateTime EventAt { get; set; }

        [DataMember(Order = 2)]
        public string Message { get; set; }

        public S3Event(string key, DateTime eventAt, string message)
        {
            Key = key;
            EventAt = eventAt;
            Message = message;
        }
    }
}
