using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Mentoragente.API.Middleware;
using Mentoragente.API.Models;
using Microsoft.Extensions.Hosting;

namespace Mentoragente.Tests.API.Middleware;

public class GlobalExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly DefaultHttpContext _httpContext;
    private readonly RequestDelegate _next;

    public GlobalExceptionHandlingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
        _next = (context) => Task.CompletedTask;
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleArgumentException()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new ArgumentException("Invalid parameter"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        _httpContext.Response.ContentType.Should().Be("application/json");

        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse.Should().NotBeNull();
        errorResponse!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        errorResponse.Title.Should().Be("Bad Request");
        errorResponse.Detail.Should().Be("Invalid parameter");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleInvalidOperationException_NotFound()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new InvalidOperationException("User with ID not found"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Status.Should().Be((int)HttpStatusCode.NotFound);
        errorResponse.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleInvalidOperationException_Conflict()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new InvalidOperationException("User already exists"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Status.Should().Be((int)HttpStatusCode.Conflict);
        errorResponse.Title.Should().Be("Conflict");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleKeyNotFoundException()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new KeyNotFoundException("Resource not found"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Status.Should().Be((int)HttpStatusCode.NotFound);
        errorResponse.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleUnauthorizedAccessException()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new UnauthorizedAccessException("Access denied"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        errorResponse.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleHttpRequestException()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new HttpRequestException("External API error"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Status.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        errorResponse.Title.Should().Be("Service Unavailable");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleGenericException()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new Exception("Unexpected error"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        errorResponse.Title.Should().Be("Internal Server Error");
        errorResponse.Extensions.Should().NotBeNull();
        errorResponse.Extensions!.Should().ContainKey("stackTrace");
        errorResponse.Extensions.Should().ContainKey("exceptionType");
    }

    [Fact]
    public async Task InvokeAsync_ShouldHideInternalDetailsInProduction()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Production);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new Exception("Sensitive internal error"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.Detail.Should().Be("An error occurred while processing your request");
        errorResponse.Detail.Should().NotContain("Sensitive");
        errorResponse.Extensions.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeTraceId()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            context => throw new Exception("Test error"),
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        errorResponse!.TraceId.Should().NotBeNullOrEmpty();
        errorResponse.TraceId.Should().Be(_httpContext.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotHandleException_WhenNoExceptionThrown()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var middleware = new GlobalExceptionHandlingMiddleware(
            _next,
            _mockLogger.Object,
            _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(200);
        _httpContext.Response.ContentType.Should().BeNull();
    }

    private async Task<string> GetResponseBody()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}


