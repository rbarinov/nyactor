using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NYActor.OpenTelemetry;

public static class OpenTelemetryActorNodeBuilderExtensions
{
    public static ActorNodeBuilder AddOpenTelemetryTracing(
        this ActorNodeBuilder actorNodeBuilder,
        string host,
        int port
    )
    {
        var assembly = Assembly.GetEntryAssembly()
            ?.GetName()
            ?.Name
            ?.ToLowerInvariant();

        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(assembly)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: assembly)
            )
            .AddJaegerExporter(
                o =>
                {
                    o.AgentHost = host;
                    o.AgentPort = port;
                }
            )
            .Build();

        var activitySource = new ActivitySource(assembly);

        return actorNodeBuilder.AddGenericTracing(
            (actorExecutionContext, activityName) =>
            {
                Activity.Current = null;
                var newActorExecutionContext = actorExecutionContext;
                var context = actorExecutionContext?.To<ScopedExecutionContext>();

                var activityContext = context != null
                    ? (ActivityContext?) new ActivityContext(
                        ActivityTraceId.CreateFromString(context.Scope["x-b3-traceid"]),
                        ActivitySpanId.CreateFromString(context.Scope["x-b3-spanid"]),
                        ActivityTraceFlags.Recorded
                    )
                    : null;

                var headers = actorExecutionContext?.To<ScopedExecutionContext>()
                    ?.Scope;

                var activity = activityContext != null
                    ? activitySource.StartActivity(
                        activityName,
                        ActivityKind.Server,
                        activityContext.Value
                    )
                    : activitySource.StartActivity(
                        activityName,
                        ActivityKind.Server
                    );

                if (activity != null)
                {
                    if (headers != null)
                    {
                        var updated = new Dictionary<string, string>(headers)
                        {
                            ["x-b3-traceid"] = activity.TraceId.ToString(),
                            ["x-b3-spanid"] = activity.SpanId.ToString()
                        };

                        newActorExecutionContext = new ScopedExecutionContext(updated);
                    }
                    else
                    {
                        var updated = new Dictionary<string, string>
                        {
                            ["x-request-id"] = Guid.NewGuid()
                                .ToString(),
                            ["x-b3-traceid"] = activity.TraceId.ToString(),
                            ["x-b3-spanid"] = activity.SpanId.ToString(),
                            ["x-b3-sampled"] = "1"
                        };

                        newActorExecutionContext = new ScopedExecutionContext(updated);
                    }
                }

                return (actorExecutionContext: newActorExecutionContext, new OpenTelemetryTracingActivity(activity));
            }
        );
    }
}

public class OpenTelemetryTracingActivity : ITracingActivity
{
    private readonly Activity _activity;

    public OpenTelemetryTracingActivity(Activity activity)
    {
        _activity = activity;
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }

    public void SetError(Exception exception, string message)
    {
        _activity?.RecordException(exception);
        _activity?.SetStatus(Status.Error.WithDescription(message));
    }
}
