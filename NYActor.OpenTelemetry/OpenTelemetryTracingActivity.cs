using System.Diagnostics;
using OpenTelemetry.Trace;

namespace NYActor.OpenTelemetry;

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
