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
        using var listener = CreateListener();
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
        using var listener = CreateListener();
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

    private static ActivityListener CreateListener() => new()
    {
        ShouldListenTo = source => source.Name == SignalRActivitySourceName,
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
    };
}