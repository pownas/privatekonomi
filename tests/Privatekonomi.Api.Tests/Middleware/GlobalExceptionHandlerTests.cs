using System.Diagnostics;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using Privatekonomi.Api.Middleware;

namespace Privatekonomi.Api.Tests.Middleware;

[TestClass]
public sealed class GlobalExceptionHandlerTests
{
    [TestMethod]
    public async Task TryHandleAsyncHidesInternalServerErrorDetailsAndReturnsTraceId()
    {
        using var activity = new Activity("exception-test");
        activity.Start();
        var expectedTraceId = activity.Id;
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new IOException("private database details"),
            CancellationToken.None);

        Assert.IsTrue(handled);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.AreEqual(expectedTraceId, response.RootElement.GetProperty("traceId").GetString());
        Assert.AreNotEqual("private database details", response.RootElement.GetProperty("detail").GetString());
    }
}