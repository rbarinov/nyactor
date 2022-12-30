create table if not exists streams
(
    stream_id varchar(1024) not null primary key,
    version   int          not null
    );

create table if not exists events
(
    stream_id      varchar(1024) not null,
    version        bigint       not null,
    global_version bigint       not null,
    type           varchar(512),
    payload        bytea,
    primary key (stream_id, version)
    );

create sequence if not exists global_version start 1;

create type event_container as
    (
    type    varchar(512),
    payload bytea
    );

create or replace procedure append_events(
    in_stream_id text,
    in_expected_version bigint,
    in_events event_container[]
)
    language plpgsql
as
$$
declare
updated_count int;
    valid_version bigint;
begin
    if in_events is null or array_length(in_events, 1) = 0 then
        raise 'no events to append';
end if;

    if in_expected_version = -1 then
begin
insert into streams (stream_id, version)
values (in_stream_id, -1 + array_length(in_events, 1));
exception
            when unique_violation then
select version into valid_version from streams where stream_id = in_stream_id;
raise 'invalid expected version. current version is %', valid_version;
end;
else
update streams
set version = in_expected_version + array_length(in_events, 1)
where stream_id = in_stream_id
  and version = in_expected_version;

get diagnostics updated_count = row_count;

if updated_count <> 1 then
select version into valid_version from streams where stream_id = in_stream_id;
raise 'invalid expected version. current version is %', coalesce(valid_version, -1);
end if;
end if;

insert into events
(stream_id, version, global_version, type, payload)
select in_stream_id,
       in_expected_version::bigint + t.ordinality,
    nextval('global_version'),
       t.type,
       t.payload
from (select "type", "payload", ordinality
      from unnest(in_events) with ordinality) t;

perform pg_notify('sub', currval('global_version')::text);

commit;
end;
$$;
