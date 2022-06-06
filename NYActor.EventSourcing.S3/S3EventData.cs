using System.Runtime.Serialization;

namespace NYActor.EventSourcing.S3;

[DataContract]
public class S3EventData
{
    [DataMember(Order = 0)]
    public long Position { get; }

    [DataMember(Order = 1)]
    public string EventTypeName { get; }

    [DataMember(Order = 2)]
    public byte[] Event { get; }

    public S3EventData(long position, string eventTypeName, byte[] @event)
    {
        Position = position;
        EventTypeName = eventTypeName;
        Event = @event;
    }
}
