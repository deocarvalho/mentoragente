using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Mentoragente.Domain.DTOs;

namespace Mentoragente.API.Middleware;

/// <summary>
/// Rate limiting middleware for enrollment endpoints based on phone number
/// Limits: 5 requests per hour per phone number
/// </summary>
public class PhoneNumberRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PhoneNumberRateLimitMiddleware> _logger;
    
    // In-memory store: phone number -> list of request timestamps
    private static readonly ConcurrentDictionary<string, List<DateTime>> _requestHistory = new();
    private static readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private static DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly object _cleanupLock = new();

    private const int MaxRequestsPerHour = 5;
    private const int StatusCodeTooManyRequests = 429;

    public PhoneNumberRateLimitMiddleware(
        RequestDelegate next,
        ILogger<PhoneNumberRateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to enrollment endpoints
        if (!context.Request.Path.StartsWithSegments("/api/Enrollments"))
        {
            await _next(context);
            return;
        }

        // Only apply to POST requests (CreateEnrollment)
        if (context.Request.Method != "POST")
        {
            await _next(context);
            return;
        }

        // Extract phone number from request body
        string? phoneNumber = null;
        try
        {
            context.Request.EnableBuffering();
            var originalBodyStream = context.Request.Body;
            
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                var request = JsonSerializer.Deserialize<CreateEnrollmentRequestDto>(body);
                phoneNumber = request?.PhoneNumber;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract phone number for rate limiting");
            // If we can't extract phone number, allow the request (fail open)
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            // No phone number found, allow request (validation will catch it)
            await _next(context);
            return;
        }

        // Cleanup old entries periodically
        CleanupOldEntries();

        // Check rate limit
        var now = DateTime.UtcNow;
        var key = phoneNumber.ToLowerInvariant();
        
        var requests = _requestHistory.GetOrAdd(key, _ => new List<DateTime>());
        
        bool rateLimitExceeded;
        int retryAfterSeconds = 0;
        
        lock (requests)
        {
            // Remove requests older than 1 hour
            requests.RemoveAll(timestamp => now - timestamp > TimeSpan.FromHours(1));
            
            // Check if limit exceeded
            if (requests.Count >= MaxRequestsPerHour)
            {
                var oldestRequest = requests.Min();
                var retryAfter = TimeSpan.FromHours(1) - (now - oldestRequest);
                retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
                rateLimitExceeded = true;
            }
            else
            {
                // Add current request
                requests.Add(now);
                rateLimitExceeded = false;
            }
        }

        // Handle rate limit response outside of lock
        if (rateLimitExceeded)
        {
            _logger.LogWarning(
                "Rate limit exceeded for phone number {PhoneNumber}. Requests: {Count}/{Max}. Retry after {RetryAfter} seconds",
                phoneNumber, MaxRequestsPerHour, MaxRequestsPerHour, retryAfterSeconds);

            context.Response.StatusCode = StatusCodeTooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Retry-After", retryAfterSeconds.ToString(CultureInfo.InvariantCulture));
            
            var errorResponse = new
            {
                success = false,
                message = $"Rate limit exceeded. Maximum {MaxRequestsPerHour} enrollments per hour per phone number. Please try again in {retryAfterSeconds} seconds.",
                retryAfter = retryAfterSeconds
            };

            var json = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(json);
            return;
        }

        // Call next middleware
        await _next(context);
    }

    private void CleanupOldEntries()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < _cleanupInterval)
        {
            return;
        }

        lock (_cleanupLock)
        {
            if (now - _lastCleanup < _cleanupInterval)
            {
                return;
            }

            var keysToRemove = new List<string>();
            foreach (var kvp in _requestHistory)
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveAll(timestamp => now - timestamp > TimeSpan.FromHours(1));
                    if (kvp.Value.Count == 0)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                _requestHistory.TryRemove(key, out _);
            }

            _lastCleanup = now;
        }
    }
}

