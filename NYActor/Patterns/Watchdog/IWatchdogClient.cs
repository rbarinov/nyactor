namespace NYActor.Patterns.Watchdog
{
    public interface IWatchdogClient : IActor
    {
        public Task Ping() => Task.CompletedTask;
    }
}