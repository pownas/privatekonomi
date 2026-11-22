using System.Diagnostics;
using OpenTelemetry;

namespace Privatekonomi.ServiceDefaults.Telemetry;

/// <summary>
/// Prevents high-volume Blazor circuit and MudBlazor event spans from flooding exported traces.
/// </summary>
public sealed class BlazorCircuitNoiseFilter : BaseProcessor<Activity>
{
    private const string SignalRActivitySourceName = "Microsoft.AspNetCore.SignalR.Server";
    private const string ComponentHubActivityPrefix = "Microsoft.AspNetCore.Components.Server.ComponentHub/";
    private const string MudBlazorEventPrefix = "Event on";
    private const string MudBlazorActivityMarker = "MudBlazor.";

    public override void OnEnd(Activity activity)
    {
        var isComponentHubActivity = activity.Source.Name == SignalRActivitySourceName &&
                                     activity.DisplayName.StartsWith(ComponentHubActivityPrefix, StringComparison.Ordinal);
        var isMudBlazorEvent = activity.DisplayName.StartsWith(MudBlazorEventPrefix, StringComparison.Ordinal) &&
                               activity.DisplayName.Contains(MudBlazorActivityMarker, StringComparison.Ordinal);

        if (isComponentHubActivity || isMudBlazorEvent)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}