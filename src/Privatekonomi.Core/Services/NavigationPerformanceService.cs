using System.Diagnostics;

namespace Privatekonomi.Core.Services;

/// <summary>
/// Service for tracking navigation performance metrics from menu click to page render
/// </summary>
public class NavigationPerformanceService : INavigationPerformanceService, IDisposable
{
    public const string ActivitySourceName = "Privatekonomi.Navigation";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private readonly Dictionary<string, NavigationMetric> _activeNavigations = new();
    private readonly object _lock = new();

    public void StartNavigation(string targetUrl, string sourceName)
    {
        lock (_lock)
        {
            StopActiveNavigations("Superseded by another navigation");

            var normalizedTargetUrl = NormalizeTargetUrl(targetUrl);
            var navigationId = Guid.NewGuid().ToString();
            var metric = new NavigationMetric
            {
                NavigationId = navigationId,
                TargetUrl = normalizedTargetUrl,
                SourceName = sourceName,
                StartTime = DateTimeOffset.UtcNow,
                Activity = ActivitySource.StartActivity("Navigation", ActivityKind.Internal)
            };

            metric.Activity?.SetTag("navigation.target_url", normalizedTargetUrl);
            metric.Activity?.SetTag("navigation.source", sourceName);
            metric.Activity?.SetTag("navigation.id", navigationId);

            _activeNavigations[normalizedTargetUrl] = metric;
        }
    }

    public Activity? StartPageRender(string targetUrl, string pageName)
    {
        lock (_lock)
        {
            var normalizedTargetUrl = NormalizeTargetUrl(targetUrl);
            if (!_activeNavigations.TryGetValue(normalizedTargetUrl, out var metric))
            {
                return null;
            }

            var activity = ActivitySource.StartActivity(
                "Page.Render",
                ActivityKind.Internal,
                metric.Activity?.Context ?? default);
            activity?.SetTag("navigation.target_url", normalizedTargetUrl);
            activity?.SetTag("navigation.page_name", pageName);
            metric.PageRenderActivity = activity;
            metric.PageRenderStarted = true;
            return activity;
        }
    }

    public void CompletePageRender(string targetUrl, string pageName, Activity? renderActivity)
    {
        lock (_lock)
        {
            var normalizedTargetUrl = NormalizeTargetUrl(targetUrl);
            renderActivity?.SetStatus(ActivityStatusCode.Ok);
            renderActivity?.Stop();

            if (!_activeNavigations.TryGetValue(normalizedTargetUrl, out var metric))
            {
                return;
            }

            metric.PageRenderCompleted = true;
            metric.PageName = pageName;
            CompleteNavigationCore(normalizedTargetUrl, metric, pageName);
        }
    }

    public void CompleteNavigation(string targetUrl, string pageName)
    {
        lock (_lock)
        {
            var normalizedTargetUrl = NormalizeTargetUrl(targetUrl);
            if (_activeNavigations.TryGetValue(normalizedTargetUrl, out var metric))
            {
                metric.PageName = pageName;
                if (!metric.PageRenderStarted || metric.PageRenderCompleted)
                {
                    CompleteNavigationCore(normalizedTargetUrl, metric, pageName);
                }
            }
        }
    }

    public void CancelNavigation(string targetUrl)
    {
        lock (_lock)
        {
            var normalizedTargetUrl = NormalizeTargetUrl(targetUrl);
            if (_activeNavigations.TryGetValue(normalizedTargetUrl, out var metric))
            {
                metric.PageRenderActivity?.Stop();
                metric.Activity?.SetStatus(ActivityStatusCode.Error, "Navigation cancelled");
                metric.Activity?.Stop();
                _activeNavigations.Remove(normalizedTargetUrl);
            }
        }
    }

    public NavigationMetric? GetActiveNavigation(string targetUrl)
    {
        lock (_lock)
        {
            return _activeNavigations.TryGetValue(NormalizeTargetUrl(targetUrl), out var metric) ? metric : null;
        }
    }

    public IReadOnlyList<NavigationMetric> GetAllActiveNavigations()
    {
        lock (_lock)
        {
            return _activeNavigations.Values.ToList();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            StopActiveNavigations("Navigation service disposed before completion");
        }

        GC.SuppressFinalize(this);
    }

    private void StopActiveNavigations(string reason)
    {
        foreach (var metric in _activeNavigations.Values)
        {
            metric.PageRenderActivity?.Stop();
            metric.Activity?.SetStatus(ActivityStatusCode.Error, reason);
            metric.Activity?.Stop();
        }

        _activeNavigations.Clear();
    }

    private void CompleteNavigationCore(string targetUrl, NavigationMetric metric, string pageName)
    {
        metric.EndTime = DateTimeOffset.UtcNow;
        metric.PageName = pageName;
        metric.DurationMs = (metric.EndTime.Value - metric.StartTime).TotalMilliseconds;

        metric.Activity?.SetTag("navigation.page_name", pageName);
        metric.Activity?.SetTag("navigation.duration_ms", metric.DurationMs);
        metric.Activity?.SetStatus(ActivityStatusCode.Ok);

        using (var completionActivity = ActivitySource.StartActivity(
                   "NavigationCompleted",
                   ActivityKind.Internal,
                   metric.Activity?.Context ?? default))
        {
            completionActivity?.SetTag("navigation.target_url", targetUrl);
            completionActivity?.SetTag("navigation.page_name", pageName);
            completionActivity?.SetTag("navigation.duration_ms", metric.DurationMs);
            completionActivity?.SetTag("navigation.source", metric.SourceName);
        }

        metric.Activity?.Stop();
        _activeNavigations.Remove(targetUrl);
    }

    private static string NormalizeTargetUrl(string targetUrl)
    {
        var queryIndex = targetUrl.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            targetUrl = targetUrl[..queryIndex];
        }

        var fragmentIndex = targetUrl.IndexOf('#', StringComparison.Ordinal);
        if (fragmentIndex >= 0)
        {
            targetUrl = targetUrl[..fragmentIndex];
        }

        return targetUrl.Trim('/');
    }
}

public class NavigationMetric
{
    public string NavigationId { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string? PageName { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public double? DurationMs { get; set; }
    public Activity? Activity { get; set; }
    public Activity? PageRenderActivity { get; set; }
    public bool PageRenderStarted { get; set; }
    public bool PageRenderCompleted { get; set; }
}
