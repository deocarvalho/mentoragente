using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Mentoragente.Infrastructure.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;

namespace Mentoragente.Tests.Infrastructure.Services;

public class EvolutionAPIServiceTests
{
    private readonly Mock<ILogger<EvolutionAPIService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IMentorshipRepository> _mockMentorshipRepository;
    private readonly HttpClient _httpClient;

    public EvolutionAPIServiceTests()
    {
        _mockLogger = new Mock<ILogger<EvolutionAPIService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockMentorshipRepository = new Mock<IMentorshipRepository>();
        _httpClient = new HttpClient();
    }

    [Fact]
    public async Task SendMessageAsync_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var phoneNumber = "5511999999999";
        var message = "Test message";
        
        _mockConfiguration.Setup(c => c["EvolutionAPI:BaseUrl"]).Returns("https://evolution-api.example.com");
        
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            EvolutionApiKey = "test-api-key",
            EvolutionInstanceName = "test-instance"
        };
        
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);
        
        var handler = MockHttpMessageHandler.CreateSuccessHandler("{\"success\": true}");
        var httpClient = new HttpClient(handler);
        var service = new EvolutionAPIService(httpClient, _mockConfiguration.Object, _mockMentorshipRepository.Object, _mockLogger.Object);

        // Act
        var result = await service.SendMessageAsync(phoneNumber, message, mentorshipId);

        // Assert
        result.Should().BeTrue();
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByIdAsync(mentorshipId), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowWhenMentorshipNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var phoneNumber = "5511999999999";
        var message = "Test message";
        
        _mockConfiguration.Setup(c => c["EvolutionAPI:BaseUrl"]).Returns("https://evolution-api.example.com");
        
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);
        
        var httpClient = new HttpClient();
        var service = new EvolutionAPIService(httpClient, _mockConfiguration.Object, _mockMentorshipRepository.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.SendMessageAsync(phoneNumber, message, mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mentorship {mentorshipId} not found*");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowWhenApiKeyNotConfigured()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var phoneNumber = "5511999999999";
        var message = "Test message";
        
        _mockConfiguration.Setup(c => c["EvolutionAPI:BaseUrl"]).Returns("https://evolution-api.example.com");
        
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            EvolutionApiKey = "", // Empty API key
            EvolutionInstanceName = "test-instance"
        };
        
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);
        
        var httpClient = new HttpClient();
        var service = new EvolutionAPIService(httpClient, _mockConfiguration.Object, _mockMentorshipRepository.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.SendMessageAsync(phoneNumber, message, mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Evolution API Key not configured for mentorship {mentorshipId}*");
    }

    [Fact]
    public async Task SendMessageAsync_ShouldThrowWhenInstanceNameNotConfigured()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var phoneNumber = "5511999999999";
        var message = "Test message";
        
        _mockConfiguration.Setup(c => c["EvolutionAPI:BaseUrl"]).Returns("https://evolution-api.example.com");
        
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            EvolutionApiKey = "test-api-key",
            EvolutionInstanceName = "" // Empty instance name
        };
        
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);
        
        var httpClient = new HttpClient();
        var service = new EvolutionAPIService(httpClient, _mockConfiguration.Object, _mockMentorshipRepository.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.SendMessageAsync(phoneNumber, message, mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Evolution Instance Name not configured for mentorship {mentorshipId}*");
    }
}

