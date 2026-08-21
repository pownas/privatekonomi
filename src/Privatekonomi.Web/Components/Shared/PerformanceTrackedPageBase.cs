using Microsoft.AspNetCore.Components;
using Privatekonomi.Core.Services;
using Privatekonomi.Web.Components.Base;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1716 // 'Shared' is a valid Blazor convention for component namespace
namespace Privatekonomi.Web.Components.Shared;
#pragma warning restore CA1716

/// <summary>
/// Base component for pages that should report navigation completion metrics
/// </summary>
public abstract class PerformanceTrackedPageBase : TrackedComponentBase
{
    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected INavigationPerformanceService NavigationPerformanceService { get; set; } = default!;

    protected abstract string PageName { get; }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            var currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            var cleanUrl = CleanUrl(currentUrl);
            NavigationPerformanceService.CompleteNavigation(cleanUrl, PageName);
        }
    }

    private static string CleanUrl(string url)
    {
        var queryIndex = url.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            url = url[..queryIndex];
        }

        var fragmentIndex = url.IndexOf('#', StringComparison.Ordinal);
        if (fragmentIndex >= 0)
        {
            url = url[..fragmentIndex];
        }

        return url.Trim('/');
    }
}
