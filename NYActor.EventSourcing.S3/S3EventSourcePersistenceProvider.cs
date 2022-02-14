using System.Net;
using System.Reactive.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using MessagePack;

namespace NYActor.EventSourcing.S3;

public class S3EventSourcePersistenceProvider :
    IEventSourcePersistenceProvider
{
    private readonly AmazonS3Client _client;
    private readonly string _space;
    private readonly string _directory;

    public S3EventSourcePersistenceProvider(
        AmazonS3Client client,
        string space,
        string directory
    )
    {
        _client = client;
        _space = space;
        _directory = directory;
    }

    public async Task PersistEventsAsync(
        Type eventSourcePersistedActorType,
        string key,
        long expectedVersion,
        IEnumerable<object> events
    )
    {
        var fileName = GetStreamName(eventSourcePersistedActorType, key);

        var meta = await GetMetaData(_directory, fileName);
        var fileData = new List<S3EventData>();
        var position = -1L;

        if (meta != null)
        {
            await using var download = await Download(_directory, fileName);

            var stored = await MessagePackSerializer.Typeless.DeserializeAsync(download);

            if (stored is List<S3EventData> storedEvents)
            {
                fileData.AddRange(storedEvents);
                position = storedEvents.Count - 1;
            }
        }

        if (position != expectedVersion)
        {
            throw new S3PersistenceConcurrencyException(fileName, position, expectedVersion);
        }

        fileData.AddRange(
            events.Select(
                (e, i) => new S3EventData(position++, e)
            )
        );

        await using var file = new MemoryStream(MessagePackSerializer.Typeless.Serialize(fileData));

        await Upload(_directory, fileName, file);
    }

    protected virtual string GetStreamName(Type eventSourcePersistedActorType, string key)
    {
        return $"{eventSourcePersistedActorType.FullName}-{key}";
    }

    public IObservable<EventSourceEventContainer> ObservePersistedEvents(
        Type eventSourcePersistedActorType,
        string key
    )
    {
        var fileName = GetStreamName(eventSourcePersistedActorType, key);

        return Observable
            .FromAsync(
                async () =>
                {
                    var meta = await GetMetaData(_directory, fileName);

                    if (meta != null)
                    {
                        await using var stream = await Download(_directory, fileName);

                        var events = await MessagePackSerializer.Typeless.DeserializeAsync(stream);

                        return events;
                    }

                    return null;
                }
            )
            .OfType<IEnumerable<S3EventData>>()
            .SelectMany(
                e => e.Select(
                    ev => new EventSourceEventContainer(
                        ev.Position.ToString(),
                        ev.Data
                    )
                )
            );
    }

    public IObservable<EventSourceEventContainer> ObserveAllEvents(string fromPosition)
    {
        throw new NotSupportedException();
    }

    private async Task<GetObjectMetadataResponse> GetMetaData(
        string directory,
        string fileName
    )
    {
        try
        {
            var req = new GetObjectMetadataRequest()
            {
                BucketName = _space + "/" + directory,
                Key = fileName
            };

            var objRes = await _client.GetObjectMetadataAsync(req);

            if (objRes.HttpStatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            return objRes;
        }
        catch (AmazonS3Exception)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task Upload(
        string directory,
        string fileName,
        Stream file
    )
    {
        try
        {
            var fileTransferUtility = new TransferUtility(_client);

            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                InputStream = file,
                BucketName = _space + "/" + directory,
                Key = fileName,
            };

            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
        }
        catch (AmazonS3Exception)
        {
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private async Task<Stream> Download(
        string directory,
        string fileName
    )
    {
        try
        {
            var req = new GetObjectRequest()
            {
                BucketName = _space + "/" + directory,
                Key = fileName
            };

            var objRes = await _client.GetObjectAsync(req);

            if (objRes.HttpStatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            return objRes.ResponseStream;
        }
        catch (AmazonS3Exception)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}