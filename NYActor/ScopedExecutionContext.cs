namespace NYActor;

public class ScopedExecutionContext : ActorExecutionContext
{
    public ScopedExecutionContext(Dictionary<string, string> scope)
    {
        Scope = scope;
    }

    public Dictionary<string, string> Scope { get; }
}
