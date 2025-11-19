using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Application.Services;

public interface IPhoneNumberValidator
{
    bool IsValidPhoneNumber(string phoneNumber);
    string? NormalizePhoneNumber(string phoneNumber);
}

public class PhoneNumberValidator : IPhoneNumberValidator
{
    private readonly ILogger<PhoneNumberValidator> _logger;
    
    // Regex para números de telefone: aceita números com ou sem código do país
    // Formato esperado: opcionalmente começa com +, seguido de 10-15 dígitos
    private static readonly Regex PhoneNumberRegex = new(@"^\+?[1-9]\d{9,14}$", RegexOptions.Compiled);

    public PhoneNumberValidator(ILogger<PhoneNumberValidator> logger)
    {
        _logger = logger;
    }

    public bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove espaços, hífens, parênteses e outros caracteres comuns
        var normalized = NormalizePhoneNumber(phoneNumber);
        if (normalized == null)
            return false;

        return PhoneNumberRegex.IsMatch(normalized);
    }

    public string? NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove espaços, hífens, parênteses, pontos e outros caracteres não numéricos (exceto +)
        var normalized = Regex.Replace(phoneNumber, @"[^\d+]", "");
        
        // Se não começa com +, adiciona se necessário (assume código do país)
        if (!normalized.StartsWith("+") && normalized.Length >= 10)
        {
            // Se começa com 0, remove (código de área local)
            if (normalized.StartsWith("0"))
            {
                normalized = normalized.Substring(1);
            }
        }

        return normalized.Length >= 10 ? normalized : null;
    }
}

