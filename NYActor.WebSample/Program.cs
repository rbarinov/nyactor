using NYActor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddActorSystem(
    e =>
    {
        e.WithActorDeactivationTimeout(TimeSpan.FromSeconds(30));
    });
builder.Services.AddSingleton<ITime>(new Time());

var app = builder.Build();

app.MapGet(
    "/",
    async req =>
    {
        var node = req.RequestServices.GetService<IActorSystem>();
        var time = req.RequestServices.GetService<ITime>();

        var res = await node.GetActor<MyActor>("key")
            .InvokeAsync(e => e.Get());

        await req.Response.WriteAsync($"Hello World (http {time.Get()}) actor: {res}!");
    }
);

app.Run();

class MyActor : Actor
{
    private readonly ITime _time;

    public MyActor(ITime time)
    {
        _time = time;
    }

    private int i = 0;

    public Task<string> Get() =>
        Task.FromResult($"{++i} = {_time.Get()}");
}

interface ITime
{
    DateTime Get();
}

class Time : ITime
{
    public DateTime Get()
    {
        return DateTime.UtcNow;
    }
}
