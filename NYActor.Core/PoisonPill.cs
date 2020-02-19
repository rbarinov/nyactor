namespace NYActor.Core
{
    public class PoisonPill : MessageQueueItem
    {
        private PoisonPill()
        {
        }

        public static PoisonPill Default = new PoisonPill();
    }
}