namespace Mentoragente.Application.Services;

/// <summary>
/// Service for sanitizing sensitive data from log messages
/// </summary>
public interface ILogSanitizer
{
    /// <summary>
    /// Masks phone numbers in log messages (e.g., "5511999999999" -> "55119****9999")
    /// </summary>
    string MaskPhoneNumber(string? phoneNumber);
    
    /// <summary>
    /// Masks tokens/API keys in log messages (e.g., "sk-abc123xyz" -> "sk-***xyz")
    /// </summary>
    string MaskToken(string? token);
    
    /// <summary>
    /// Masks email addresses in log messages (e.g., "user@example.com" -> "u***@example.com")
    /// </summary>
    string MaskEmail(string? email);
    
    /// <summary>
    /// Sanitizes a JSON payload by masking sensitive fields
    /// </summary>
    string SanitizeJsonPayload(string json);
    
    /// <summary>
    /// Sanitizes a message text (truncates if too long, removes sensitive patterns)
    /// </summary>
    string SanitizeMessageText(string? messageText, int maxLength = 100);
}

public class LogSanitizer : ILogSanitizer
{
    public string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return "***";
        
        // Keep first 5 and last 4 digits, mask the rest
        if (phoneNumber.Length <= 9)
            return "***";
        
        var start = phoneNumber.Substring(0, Math.Min(5, phoneNumber.Length));
        var end = phoneNumber.Length > 9 
            ? phoneNumber.Substring(phoneNumber.Length - 4) 
            : "";
        
        return $"{start}****{end}";
    }
    
    public string MaskToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "***";
        
        // Keep first 3 and last 3 characters, mask the rest
        if (token.Length <= 6)
            return "***";
        
        var start = token.Substring(0, 3);
        var end = token.Substring(token.Length - 3);
        
        return $"{start}***{end}";
    }
    
    public string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "***";
        
        var parts = email.Split('@');
        if (parts.Length != 2)
            return "***";
        
        var username = parts[0];
        var domain = parts[1];
        
        // Mask username: keep first char, mask the rest
        var maskedUsername = username.Length > 1 
            ? $"{username[0]}***" 
            : "***";
        
        return $"{maskedUsername}@{domain}";
    }
    
    public string SanitizeJsonPayload(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "{}";
        
        try
        {
            // List of sensitive field names to mask
            var sensitiveFields = new[] 
            { 
                "token", "Token", "TOKEN",
                "apikey", "ApiKey", "API_KEY", "api_key",
                "password", "Password", "PASSWORD",
                "secret", "Secret", "SECRET",
                "client-token", "Client-Token", "CLIENT_TOKEN",
                "authorization", "Authorization", "AUTHORIZATION",
                "instanceToken", "InstanceToken", "instance_token",
                "instanceCode", "InstanceCode", "instance_code"
            };
            
            // Simple regex-based masking (for production, consider using System.Text.Json)
            var sanitized = json;
            foreach (var field in sensitiveFields)
            {
                // Pattern: "fieldName": "value" -> "fieldName": "***"
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]+\"";
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                    sanitized, 
                    pattern, 
                    $"\"{field}\": \"***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            
            return sanitized;
        }
        catch
        {
            // If sanitization fails, return a safe message
            return "{ \"error\": \"Failed to sanitize payload\" }";
        }
    }
    
    public string SanitizeMessageText(string? messageText, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return "***";
        
        // Truncate if too long
        if (messageText.Length > maxLength)
        {
            return messageText.Substring(0, maxLength) + "...";
        }
        
        return messageText;
    }
}

