# NYActor - Not yet another actor system
Slim and super-fast actor system for dotnet

# Benchmark
- Handles ~500k rps on 8-core AMD Ryzen 2700x in-memory
- Handles ~20k rps on 8-core AMD Ryzen 2700x over aspnet core REST

## Roadmap
- Multi-node mode with DHT and DNS discovery to build distributed high-scale apps

## Known issues
- Supports single-node mode only for now

## Quick start

- Install nuget package TBD

### Create your first in-memory Actor

```cs
public class SampleActor : Actor
{
    private readonly Random _random = new Random();

    public Task Nope()
    {
        return Task.CompletedTask;
    }

    public Task<int> Random()
    {
        return Task.FromResult(_random.Next());
    }
}
```

### Initialize actor system

```cs
var node = new Node()
    .RegisterActorsFromAssembly(typeof(SampleActor).Assembly);
```

### Call actor method

```cs
await node
.GetActor<SampleActor>()
.InvokeAsync(e => e.Nope());
```

```cs
var randomValue = await node
    .GetActor<SampleActor>()
    .InvokeAsync(e => e.Random());
```

## Event sourcing and NYActor

### Configure dependency injection

```cs
var userCredentials = new UserCredentials("admin", "changeit");
var connectionSettingsBuilder = ConnectionSettings.Create()
    .SetDefaultUserCredentials(userCredentials)
    .Build();

var es = EventStoreConnection.Create(connectionSettingsBuilder,
    new IPEndPoint(IPAddress.Any, 1113));

await es.ConnectAsync();

using var node = new Node()
    .ConfigureInjector(e => { e.RegisterInstance(es); })
    .RegisterActorsFromAssembly(typeof(SampleActor).Assembly);
```

### Create an eventsourced actor

```cs
public class SampleEventSourcedActor :
    EventStorePersistedActor<SampleEventSourcedState>
{
    public SampleEventSourcedActor(IEventStoreConnection eventStoreConnection) :
        base(eventStoreConnection)
    {
    }

    public async Task Append()
    {
        var @event = new SampleEvent(DateTime.UtcNow);
        await ApplyEventAsync(@event);
    }

    public Task<int> GetInfo()
    {
        var res = State.TotalEvents;
        return Task.FromResult(res);
    }
}
```

### Create a state class 

```cs
public class SampleEventSourcedState : IApplicable
{
    public int TotalEvents { get; private set; }

    public SampleEventSourcedState()
    {
        TotalEvents = 0;
    }

    public void Apply(object ev)
    {
        TotalEvents++;
    }
}
```

### Define event

```cs
public class SampleEvent
{
    public DateTime EventAt { get; }

    public SampleEvent(DateTime eventAt)
    {
        EventAt = eventAt;
    }
}
```

## Advanced actors

### Returning back to actor after external job

```cs
public class SampleActor : Actor
{
    public Task Nope()
    {
        Task.Run(async () =>
        {
            var minutesToWork = 5;

            await Task.Delay(TimeSpan.FromMinutes(minutesToWork));

            await this.Self().InvokeAsync(e => e.CompleteLongOp(minutesToWork));
        });

        return Task.CompletedTask;
    }

    public Task CompleteLongOp(int result)
    {
        // ...

        return Task.CompletedTask;
    }
}
```

### Creating a second actor inside a first one

```cs
public class SampleActor : Actor
{
    public async Task Nope()
    {
        await this.System()
            .GetActor<SampleActor>(Key + "-child")
            .InvokeAsync(e => e.ChildAction());
    }

    public Task ChildAction()
    {
        // ...
        return Task.CompletedTask;
    }
}
```

## Customizing environment

### Override actor deactivation interval

```cs
var node = new Node()
    .OverrideDefaultDeactivationInterval(TimeSpan.FromSeconds(30))
    .RegisterActorsFromAssembly(typeof(SampleActor).Assembly);
```