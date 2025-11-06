using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Mentoragente.Infrastructure.Extensions;

public static class JsonConfigurationExtensions
{
    /// <summary>
    /// Configura a serialização JSON global para serializar enums como strings.
    /// Necessário porque o Supabase PostgreSQL espera strings para tipos ENUM.
    /// </summary>
    public static void ConfigureGlobalJsonSerialization()
    {
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };
    }
}