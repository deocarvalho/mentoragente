using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Mentoragente.Application.Services;

public class WhatsAppServiceFactory : IWhatsAppServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WhatsAppServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IWhatsAppService GetService(WhatsAppProvider provider)
    {
        return provider switch
        {
            WhatsAppProvider.EvolutionAPI => _serviceProvider.GetRequiredService<IEvolutionAPIService>(),
            WhatsAppProvider.ZApi => _serviceProvider.GetRequiredService<IZApiService>(),
            WhatsAppProvider.OfficialWhatsApp => throw new NotSupportedException("Official WhatsApp API is not yet implemented"),
            _ => throw new NotSupportedException($"Provider {provider} is not supported")
        };
    }

    public IWhatsAppService GetServiceForMentorship(Mentorship mentorship)
    {
        return GetService(mentorship.WhatsAppProvider);
    }
}

