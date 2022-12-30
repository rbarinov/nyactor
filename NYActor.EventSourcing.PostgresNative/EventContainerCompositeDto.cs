namespace NYActor.EventSourcing.PostgresqlNative;

public class EventContainerCompositeDto
{
    public string Type { get; set; }
    public byte[] Payload { get; set; }
}
