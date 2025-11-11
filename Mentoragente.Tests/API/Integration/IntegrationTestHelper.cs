using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mentoragente.API;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.DTOs;
using FluentValidation;

namespace Mentoragente.Tests.API.Integration;

/// <summary>
/// Helper class for setting up integration tests with mocked services
/// </summary>
public class IntegrationTestHelper
{
    public WebApplicationFactory<Program> Factory { get; }
    public HttpClient Client { get; }
    
    // Mocked services
    public Mock<IUserService> MockUserService { get; }
    public Mock<IMentorshipService> MockMentorshipService { get; }
    public Mock<IAgentSessionService> MockAgentSessionService { get; }
    public Mock<IMessageProcessor> MockMessageProcessor { get; }
    public Mock<IEvolutionAPIService> MockEvolutionAPIService { get; }
    public Mock<IOpenAIAssistantService> MockOpenAIAssistantService { get; }
    
    // Mocked validators
    public Mock<IValidator<CreateEnrollmentRequestDto>> MockEnrollmentValidator { get; }
    public Mock<IValidator<CreateUserRequestDto>> MockCreateUserValidator { get; }
    public Mock<IValidator<UpdateUserRequestDto>> MockUpdateUserValidator { get; }
    public Mock<IValidator<CreateMentorshipRequestDto>> MockCreateMentorshipValidator { get; }
    public Mock<IValidator<UpdateMentorshipRequestDto>> MockUpdateMentorshipValidator { get; }
    public Mock<IValidator<CreateAgentSessionRequestDto>> MockCreateAgentSessionValidator { get; }
    public Mock<IValidator<UpdateAgentSessionRequestDto>> MockUpdateAgentSessionValidator { get; }

    public IntegrationTestHelper()
    {
        // Initialize mocks
        MockUserService = new Mock<IUserService>();
        MockMentorshipService = new Mock<IMentorshipService>();
        MockAgentSessionService = new Mock<IAgentSessionService>();
        MockMessageProcessor = new Mock<IMessageProcessor>();
        MockEvolutionAPIService = new Mock<IEvolutionAPIService>();
        MockOpenAIAssistantService = new Mock<IOpenAIAssistantService>();
        MockEnrollmentValidator = new Mock<IValidator<CreateEnrollmentRequestDto>>();
        MockCreateUserValidator = new Mock<IValidator<CreateUserRequestDto>>();
        MockUpdateUserValidator = new Mock<IValidator<UpdateUserRequestDto>>();
        MockCreateMentorshipValidator = new Mock<IValidator<CreateMentorshipRequestDto>>();
        MockUpdateMentorshipValidator = new Mock<IValidator<UpdateMentorshipRequestDto>>();
        MockCreateAgentSessionValidator = new Mock<IValidator<CreateAgentSessionRequestDto>>();
        MockUpdateAgentSessionValidator = new Mock<IValidator<UpdateAgentSessionRequestDto>>();

        // Setup default validators to pass validation
        var validationResult = new FluentValidation.Results.ValidationResult();
        MockEnrollmentValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateEnrollmentRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        MockCreateUserValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateUserRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        MockUpdateUserValidator
            .Setup(x => x.ValidateAsync(It.IsAny<UpdateUserRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        MockCreateMentorshipValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateMentorshipRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        MockUpdateMentorshipValidator
            .Setup(x => x.ValidateAsync(It.IsAny<UpdateMentorshipRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        MockCreateAgentSessionValidator
            .Setup(x => x.ValidateAsync(It.IsAny<CreateAgentSessionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        MockUpdateAgentSessionValidator
            .Setup(x => x.ValidateAsync(It.IsAny<UpdateAgentSessionRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Create factory with service overrides
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove HttpClient registrations for services that use HttpClient
                    var httpClientDescriptors = services
                        .Where(d => d.ServiceType == typeof(IOpenAIAssistantService) || 
                                    d.ServiceType == typeof(IEvolutionAPIService))
                        .ToList();
                    foreach (var descriptor in httpClientDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Replace real services with mocks
                    services.RemoveService<IUserService>();
                    services.RemoveService<IMentorshipService>();
                    services.RemoveService<IAgentSessionService>();
                    services.RemoveService<IMessageProcessor>();
                    services.RemoveService<IEvolutionAPIService>();
                    services.RemoveService<IOpenAIAssistantService>();
                    
                    // Remove real validator implementations first
                    var validatorDescriptors = services
                        .Where(d => d.ServiceType.IsGenericType && 
                                    d.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>))
                        .ToList();
                    foreach (var descriptor in validatorDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Disable FluentValidation automatic validation in tests
                    // This prevents NullReferenceException when FluentValidation tries to resolve validators
                    services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
                    {
                        // Remove only FluentValidation validator provider, keep others (like DataAnnotations)
                        var fluentValidationProvider = options.ModelValidatorProviders
                            .FirstOrDefault(p => p.GetType().Name.Contains("FluentValidation"));
                        if (fluentValidationProvider != null)
                        {
                            options.ModelValidatorProviders.Remove(fluentValidationProvider);
                        }
                    });

                    // Register mocked validators - controllers will use these directly via DI
                    services.AddScoped<IValidator<CreateEnrollmentRequestDto>>(_ => MockEnrollmentValidator.Object);
                    services.AddScoped<IValidator<CreateUserRequestDto>>(_ => MockCreateUserValidator.Object);
                    services.AddScoped<IValidator<UpdateUserRequestDto>>(_ => MockUpdateUserValidator.Object);
                    services.AddScoped<IValidator<CreateMentorshipRequestDto>>(_ => MockCreateMentorshipValidator.Object);
                    services.AddScoped<IValidator<UpdateMentorshipRequestDto>>(_ => MockUpdateMentorshipValidator.Object);
                    services.AddScoped<IValidator<CreateAgentSessionRequestDto>>(_ => MockCreateAgentSessionValidator.Object);
                    services.AddScoped<IValidator<UpdateAgentSessionRequestDto>>(_ => MockUpdateAgentSessionValidator.Object);

                    // Register service mocks
                    services.AddScoped(_ => MockUserService.Object);
                    services.AddScoped(_ => MockMentorshipService.Object);
                    services.AddScoped(_ => MockAgentSessionService.Object);
                    services.AddScoped(_ => MockMessageProcessor.Object);
                    services.AddScoped(_ => MockEvolutionAPIService.Object);
                    services.AddScoped(_ => MockOpenAIAssistantService.Object);
                    
                    // Configure logging
                    services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.ClearProviders();
                        loggingBuilder.AddConsole();
                        loggingBuilder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
                    });
                });
            });

        Client = Factory.CreateClient();
    }

    public void Dispose()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }
}

/// <summary>
/// Extension methods for service collection
/// </summary>
public static class ServiceCollectionExtensions
{
    public static void RemoveService<T>(this IServiceCollection services) where T : class
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    public static void RemoveValidator<T>(IServiceCollection services) where T : class
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }
}

