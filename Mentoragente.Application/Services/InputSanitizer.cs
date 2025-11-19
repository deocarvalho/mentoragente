using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IInputSanitizer
{
    string SanitizeMessage(string message);
}

public class InputSanitizer : IInputSanitizer
{
    private readonly ILogger<InputSanitizer> _logger;
    
    // Remove caracteres de controle e normaliza espaços em branco
    private static readonly Regex ControlCharsRegex = new(@"[\x00-\x1F\x7F-\x9F]", RegexOptions.Compiled);
    private static readonly Regex MultipleSpacesRegex = new(@"\s+", RegexOptions.Compiled);

    public InputSanitizer(ILogger<InputSanitizer> logger)
    {
        _logger = logger;
    }

    public string SanitizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        // Remove caracteres de controle
        var sanitized = ControlCharsRegex.Replace(message, "");
        
        // Normaliza espaços em branco múltiplos
        sanitized = MultipleSpacesRegex.Replace(sanitized, " ");
        
        // Remove espaços no início e fim
        sanitized = sanitized.Trim();
        
        // Limita tamanho máximo (prevenção de DoS)
        const int maxLength = 10000;
        if (sanitized.Length > maxLength)
        {
            _logger.LogWarning("Message truncated from {OriginalLength} to {MaxLength} characters", sanitized.Length, maxLength);
            sanitized = sanitized.Substring(0, maxLength);
        }

        return sanitized;
    }
}

