namespace NYActor;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}