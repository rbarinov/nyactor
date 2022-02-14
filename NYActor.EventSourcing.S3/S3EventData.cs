using MessagePack;

namespace NYActor.EventSourcing.S3;

[MessagePackObject]
public class S3EventData
{
    [Key(0)]
    public long Position { get; }

    [Key(1)]
    public object Data { get; }

    public S3EventData(long position, object data)
    {
        Position = position;
        Data = data;
    }
}