using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Privatekonomi.Api.Controllers;

namespace Privatekonomi.Api.Tests.Infrastructure;

// Only legacy controller behavior tests opt out; security tests use the unmodified host.
public sealed class LegacyFunctionalApiWebApplicationFactory(string databaseName)
    : ApiWebApplicationFactory(databaseName)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.Configure<MvcOptions>(options => options.Conventions.Add(new EnableLegacyControllers())));
    }

    private sealed class EnableLegacyControllers : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var filter in controller.Filters.OfType<LegacyApiDisabledAttribute>().ToArray())
                    controller.Filters.Remove(filter);
            }
        }
    }
}
