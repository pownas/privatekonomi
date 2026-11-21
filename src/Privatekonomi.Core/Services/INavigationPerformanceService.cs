using System.Diagnostics;

namespace Privatekonomi.Core.Services;

public interface INavigationPerformanceService
{
    void StartNavigation(string targetUrl, string sourceName);
    Activity? StartPageRender(string targetUrl, string pageName);
    void CompletePageRender(string targetUrl, string pageName, Activity? renderActivity);
    void CompleteNavigation(string targetUrl, string pageName);
    void CancelNavigation(string targetUrl);
    NavigationMetric? GetActiveNavigation(string targetUrl);
    IReadOnlyList<NavigationMetric> GetAllActiveNavigations();
}
