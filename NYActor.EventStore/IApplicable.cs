namespace NYActor.EventStore
{
    public interface IApplicable
    {
        void Apply(object ev);
    }
}