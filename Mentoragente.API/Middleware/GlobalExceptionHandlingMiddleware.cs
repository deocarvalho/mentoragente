using System.Net;
using System.Net.Http;
using System.Text.Json;
using Mentoragente.API.Models;

namespace Mentoragente.API.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = CreateErrorResponse(context, exception);
        response.StatusCode = errorResponse.Status;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(errorResponse, options);
        await response.WriteAsync(json);
    }

    private ErrorResponse CreateErrorResponse(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var errorResponse = new ErrorResponse
        {
            TraceId = traceId
        };

        switch (exception)
        {
            case ArgumentException argEx:
                errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                errorResponse.Title = "Bad Request";
                errorResponse.Status = (int)HttpStatusCode.BadRequest;
                errorResponse.Detail = _environment.IsDevelopment() 
                    ? argEx.Message 
                    : "Invalid request parameters";
                break;

            case InvalidOperationException invalidOpEx:
                // Check if it's a "not found" scenario
                if (invalidOpEx.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                    errorResponse.Title = "Not Found";
                    errorResponse.Status = (int)HttpStatusCode.NotFound;
                    errorResponse.Detail = _environment.IsDevelopment() 
                        ? invalidOpEx.Message 
                        : "The requested resource was not found";
                }
                // Check if it's a conflict scenario (e.g., duplicate resource)
                else if (invalidOpEx.Message.Contains("already exists") || 
                    invalidOpEx.Message.Contains("duplicate") ||
                    invalidOpEx.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
                {
                    errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
                    errorResponse.Title = "Conflict";
                    errorResponse.Status = (int)HttpStatusCode.Conflict;
                    errorResponse.Detail = _environment.IsDevelopment() 
                        ? invalidOpEx.Message 
                        : "The resource already exists or conflicts with existing data";
                }
                else
                {
                    errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                    errorResponse.Title = "Bad Request";
                    errorResponse.Status = (int)HttpStatusCode.BadRequest;
                    errorResponse.Detail = _environment.IsDevelopment() 
                        ? invalidOpEx.Message 
                        : "The operation could not be completed";
                }
                break;

            case KeyNotFoundException keyNotFoundEx:
                errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                errorResponse.Title = "Not Found";
                errorResponse.Status = (int)HttpStatusCode.NotFound;
                errorResponse.Detail = _environment.IsDevelopment() 
                    ? keyNotFoundEx.Message 
                    : "The requested resource was not found";
                break;

            case UnauthorizedAccessException:
                errorResponse.Type = "https://tools.ietf.org/html/rfc7235#section-3.1";
                errorResponse.Title = "Unauthorized";
                errorResponse.Status = (int)HttpStatusCode.Unauthorized;
                errorResponse.Detail = "Authentication required";
                break;

            case NotImplementedException:
                errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2";
                errorResponse.Title = "Not Implemented";
                errorResponse.Status = (int)HttpStatusCode.NotImplemented;
                errorResponse.Detail = "This feature is not yet implemented";
                break;

            case TimeoutException:
                errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5";
                errorResponse.Title = "Request Timeout";
                errorResponse.Status = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Detail = "The request timed out";
                break;

            case HttpRequestException httpEx:
                errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3";
                errorResponse.Title = "Service Unavailable";
                errorResponse.Status = (int)HttpStatusCode.ServiceUnavailable;
                errorResponse.Detail = _environment.IsDevelopment() 
                    ? $"External service error: {httpEx.Message}" 
                    : "An external service is temporarily unavailable";
                break;

            default:
                errorResponse.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                errorResponse.Title = "Internal Server Error";
                errorResponse.Status = (int)HttpStatusCode.InternalServerError;
                errorResponse.Detail = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An error occurred while processing your request";
                
                if (_environment.IsDevelopment())
                {
                    errorResponse.Extensions = new Dictionary<string, object>
                    {
                        { "stackTrace", exception.StackTrace ?? string.Empty },
                        { "exceptionType", exception.GetType().Name }
                    };
                }
                break;
        }

        return errorResponse;
    }
}

