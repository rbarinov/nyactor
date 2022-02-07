namespace NYActor.EventSourcing
{
    public interface IApplicable
    {
        void Apply(object ev);
    }
}