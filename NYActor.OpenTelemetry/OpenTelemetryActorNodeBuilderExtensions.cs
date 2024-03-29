﻿using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NYActor.OpenTelemetry;

public static class OpenTelemetryActorNodeBuilderExtensions
{
    public static ActorSystemBuilder AddOpenTelemetryTracing(
        this ActorSystemBuilder actorSystemBuilder,
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
                    .AddService(assembly)
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

        return actorSystemBuilder.AddGenericTracing(
            (actorExecutionContext, activityName) =>
            {
                Activity.Current = null;
                var newActorExecutionContext = actorExecutionContext;
                var context = actorExecutionContext?.To<ScopedExecutionContext>();

                var activityContext = context != null && context.Scope.ContainsKey("x-b3-traceid") && context.Scope.ContainsKey("x-b3-spanid")
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
