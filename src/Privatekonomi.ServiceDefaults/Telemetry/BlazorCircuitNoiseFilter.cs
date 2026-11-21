using System.Diagnostics;
using OpenTelemetry;

namespace Privatekonomi.ServiceDefaults.Telemetry;

/// <summary>
/// Prevents the unparented Blazor circuit hub invocations from flooding exported traces.
/// </summary>
public sealed class BlazorCircuitNoiseFilter : BaseProcessor<Activity>
{
    private const string SignalRActivitySourceName = "Microsoft.AspNetCore.SignalR.Server";
    private const string ComponentHubActivityPrefix = "Microsoft.AspNetCore.Components.Server.ComponentHub/";

    public override void OnEnd(Activity activity)
    {
        if (activity.Source.Name == SignalRActivitySourceName &&
            activity.DisplayName.StartsWith(ComponentHubActivityPrefix, StringComparison.Ordinal))
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}