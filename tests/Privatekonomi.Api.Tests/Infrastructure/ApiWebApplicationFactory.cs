using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Privatekonomi.Core.Data;
using Privatekonomi.Core.Services;

namespace Privatekonomi.Api.Tests.Infrastructure;

public class ApiWebApplicationFactory(string databaseName)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PrivatekonomyContext>>();
            services.RemoveAll<IDbContextFactory<PrivatekonomyContext>>();
            services.RemoveAll<PrivatekonomyContext>();

            services.AddDbContextFactory<PrivatekonomyContext>(o => o.UseInMemoryDatabase(databaseName));

            services.RemoveAll<ICurrentUserService>();
            services.AddScoped<ICurrentUserService>(_ => new TestCurrentUserService
            {
                IsAuthenticated = false,
                UserId = null
            });
        });
    }
}