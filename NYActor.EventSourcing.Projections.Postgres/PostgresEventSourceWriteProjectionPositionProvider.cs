using Dapper;

namespace NYActor.EventSourcing.Projections.Postgres;

public class PostgresEventSourceWriteProjectionPositionProvider :
    IEventSourceWriteProjectionPositionProvider
{
    private readonly IPostgresConnectionFactory _connectionFactory;
    private readonly ITimeProvider _timeProvider;

    public PostgresEventSourceWriteProjectionPositionProvider(
        IPostgresConnectionFactory connectionFactory,
        ITimeProvider timeProvider
    )
    {
        _connectionFactory = connectionFactory;
        _timeProvider = timeProvider;
    }

    public virtual string GetProjectionName(Type eventSourceWriteProjectionType)
    {
        return eventSourceWriteProjectionType.FullName;
    }

    public async Task<EventSourceWriteProjectionState> ReadPositionAsync(Type eventSourceWriteProjectionType)
    {
        var name = GetProjectionName(eventSourceWriteProjectionType);

        await using var db = await _connectionFactory.OpenConnection();

        var syncPosition = await db.QueryFirstOrDefaultAsync<ProjectionStateDto>(
            @"-- noinspection SqlNoDataSourceInspection

select sync_position, sync_at from projection_state where projection = @projection",
            new {projection = name}
        );

        return new EventSourceWriteProjectionState(syncPosition?.SyncPosition, syncPosition?.SyncAt);
    }

    public async Task WritePositionAsync(Type eventSourceWriteProjectionType, string syncPosition)
    {
        var projectionName = GetProjectionName(eventSourceWriteProjectionType);

        await using var db = await _connectionFactory.OpenConnection();

        await db.ExecuteAsync(
            @"-- noinspection SqlNoDataSourceInspection

insert into projection_state (projection,
                              sync_position,
                              sync_at)
values (@projection,
        @syncPosition,
        @syncAt)
on conflict (projection ) do update set sync_position = @syncPosition,
                                        sync_at       = @syncAt
;",
            new {projection = projectionName, syncPosition = syncPosition, syncAt = _timeProvider.UtcNow}
        );
    }

    private class ProjectionStateDto
    {
        public string SyncPosition { get; set; }
        public DateTime SyncAt { get; set; }
    }

    public async Task InitializeDb()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        await using var db = await _connectionFactory.OpenConnection();

        await db.ExecuteAsync(
            @"
-- noinspection SqlNoDataSourceInspection

create table if not exists
    projection_state
(
    projection    varchar(512) not null primary key,
    sync_position text,
    sync_at       timestamp    not null
);
"
        );
    }
}
