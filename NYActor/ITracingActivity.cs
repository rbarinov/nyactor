namespace NYActor;

public interface ITracingActivity : IDisposable
{
    void SetError(Exception exception, string message);
}
