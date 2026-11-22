using System.Diagnostics;
using Privatekonomi.ServiceDefaults.Telemetry;

namespace Privatekonomi.Core.Tests.Services;

[TestClass]
public class BlazorCircuitNoiseFilterTests
{
    private const string SignalRActivitySourceName = "Microsoft.AspNetCore.SignalR.Server";

    [TestMethod]
    public void OnEnd_DropsBlazorComponentHubActivities()
    {
        using var listener = CreateListener(SignalRActivitySourceName);
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource(SignalRActivitySourceName);
        using var activity = source.StartActivity(
            "Microsoft.AspNetCore.Components.Server.ComponentHub/BeginInvokeDotNetFromJS");
        var filter = new BlazorCircuitNoiseFilter();

        Assert.IsNotNull(activity);
        Assert.IsTrue(activity.Recorded);

        activity.Stop();
        filter.OnEnd(activity);

        Assert.IsFalse(activity.Recorded);
    }

    [TestMethod]
    public void OnEnd_PreservesOtherSignalRActivities()
    {
        using var listener = CreateListener(SignalRActivitySourceName);
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource(SignalRActivitySourceName);
        using var activity = source.StartActivity("MyHub/ImportantOperation");
        var filter = new BlazorCircuitNoiseFilter();

        Assert.IsNotNull(activity);
        Assert.IsTrue(activity.Recorded);

        activity.Stop();
        filter.OnEnd(activity);

        Assert.IsTrue(activity.Recorded);
    }

    [TestMethod]
    public void OnEnd_DropsMudBlazorEventActivities()
    {
        const string activitySourceName = "Microsoft.AspNetCore.Components.Server.Circuits";
        using var listener = CreateListener(activitySourceName);
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource(activitySourceName);
        using var activity = source.StartActivity("Event ontransitionend MudBlazor.EventUtil+AsyncReceiver.Invoke");
        var filter = new BlazorCircuitNoiseFilter();

        Assert.IsNotNull(activity);
        Assert.IsTrue(activity.Recorded);

        activity.Stop();
        filter.OnEnd(activity);

        Assert.IsFalse(activity.Recorded);
    }

    private static ActivityListener CreateListener(string activitySourceName) => new()
    {
        ShouldListenTo = source => source.Name == activitySourceName,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
    };
}