namespace NYActor.EventSourcing.S3;

public class S3PersistenceConcurrencyException : Exception
{
    public string Stream { get; }
    public long Position { get; }
    public long ExpectedVersion { get; }

    public S3PersistenceConcurrencyException(string stream, long position, long expectedVersion)
    {
        Stream = stream;
        Position = position;
        ExpectedVersion = expectedVersion;
    }

    public override string ToString()
    {
        return
            $"{base.ToString()}, {nameof(Stream)}: {Stream}, {nameof(Position)}: {Position}, {nameof(ExpectedVersion)}: {ExpectedVersion}";
    }
}