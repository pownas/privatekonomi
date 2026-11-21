using System.Diagnostics;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Core.Tests.Services;

[TestClass]
public class NavigationPerformanceServiceTests
{
    [TestMethod]
    public void NavigationAndPageRenderActivitiesShareTraceAndParent()
    {
        var stoppedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == NavigationPerformanceService.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Add
        };
        ActivitySource.AddActivityListener(listener);

        var service = new NavigationPerformanceService();
        using var parentActivity = new Activity("ComponentHub.Navigation").SetIdFormat(ActivityIdFormat.W3C).Start();

        service.StartNavigation("/economy/transactions?filterdate=2026-10", "test");
        var navigation = service.GetActiveNavigation("economy/transactions")!.Activity!;
        var pageRender = service.StartPageRender("/economy/transactions#results", "Transactions")!;

        service.CompleteNavigation("economy/transactions", "Transactions");
        Assert.IsNotNull(service.GetActiveNavigation("/economy/transactions"));

        service.CompletePageRender("economy/transactions", "Transactions", pageRender);

        Assert.IsNull(service.GetActiveNavigation("/economy/transactions?filterdate=2026-10"));
        Assert.AreEqual(parentActivity.TraceId, navigation.TraceId);
        Assert.AreEqual(parentActivity.SpanId, navigation.ParentSpanId);
        Assert.AreEqual(navigation.TraceId, pageRender.TraceId);
        Assert.AreEqual(navigation.SpanId, pageRender.ParentSpanId);

        var completion = stoppedActivities.Single(activity => activity.OperationName == "NavigationCompleted");
        Assert.AreEqual(navigation.TraceId, completion.TraceId);
        Assert.AreEqual(navigation.SpanId, completion.ParentSpanId);
    }
}