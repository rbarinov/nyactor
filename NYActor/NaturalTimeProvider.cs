namespace NYActor;

public class NaturalTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
