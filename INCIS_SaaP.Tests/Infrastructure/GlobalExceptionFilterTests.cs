using FluentAssertions;
using INCIS_SaaP.Infrastructure.Exceptions;
using INCIS_SaaP.Infrastructure.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace INCIS_SaaP.Tests.Infrastructure;

/// <summary>
/// Tests for GlobalExceptionFilter standardized API error handling.
/// </summary>
public class GlobalExceptionFilterTests
{
    private static ExceptionContext BuildContext(string path, Exception exception, string? acceptHeader = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        if (acceptHeader != null)
            httpContext.Request.Headers["Accept"] = acceptHeader;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    private static GlobalExceptionFilter CreateFilter(string environment = "Production")
    {
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(environment);
        return new GlobalExceptionFilter(NullLogger<GlobalExceptionFilter>.Instance, env.Object);
    }

    [Fact]
    public void OnException_AppException_ReturnsMappedStatusAndCode()
    {
        var ctx = BuildContext("/api/formulas", new NotFoundException("Formula", 5));
        CreateFilter().OnException(ctx);

        ctx.ExceptionHandled.Should().BeTrue();
        var result = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(404);
        var body = result.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        body.ErrorCode.Should().Be("NOT_FOUND");
        body.Status.Should().Be(404);
    }

    [Fact]
    public void OnException_ValidationException_IncludesErrors()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = new[] { "Required" } };
        var ctx = BuildContext("/api/x", new ValidationException("Invalid", errors));

        CreateFilter().OnException(ctx);

        var body = ((ObjectResult)ctx.Result!).Value.Should().BeOfType<ApiErrorResponse>().Subject;
        body.Status.Should().Be(400);
        body.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public void OnException_GenericException_InProduction_HidesDetails()
    {
        var ctx = BuildContext("/api/x", new InvalidOperationException("secret details"));

        CreateFilter("Production").OnException(ctx);

        var body = ((ObjectResult)ctx.Result!).Value.Should().BeOfType<ApiErrorResponse>().Subject;
        body.Status.Should().Be(500);
        body.ErrorCode.Should().Be("INTERNAL_ERROR");
        body.Message.Should().Be("An unexpected error occurred.");
        body.StackTrace.Should().BeNull();
    }

    [Fact]
    public void OnException_GenericException_InDevelopment_ExposesDetails()
    {
        var ctx = BuildContext("/api/x", new InvalidOperationException("secret details"));

        CreateFilter("Development").OnException(ctx);

        var body = ((ObjectResult)ctx.Result!).Value.Should().BeOfType<ApiErrorResponse>().Subject;
        body.Message.Should().Be("secret details");
    }

    [Fact]
    public void OnException_NonApiRequest_IsIgnored()
    {
        var ctx = BuildContext("/Home/Index", new NotFoundException("x"));

        CreateFilter().OnException(ctx);

        ctx.Result.Should().BeNull();
        ctx.ExceptionHandled.Should().BeFalse();
    }

    [Fact]
    public void OnException_JsonAcceptHeader_IsTreatedAsApi()
    {
        var ctx = BuildContext("/Home/Data", new NotFoundException("x"), acceptHeader: "application/json");

        CreateFilter().OnException(ctx);

        ctx.ExceptionHandled.Should().BeTrue();
    }
}
