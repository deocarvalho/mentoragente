using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mentoragente.API.Controllers;
using Mentoragente.Application.Adapters;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using System.Security.Claims;

namespace Mentoragente.Tests.API.Controllers;

public class ZApiWebhookControllerTests
{
    private readonly Mock<IMessageProcessor> _mockMessageProcessor;
    private readonly Mock<IWhatsAppServiceFactory> _mockWhatsAppServiceFactory;
    private readonly Mock<IZApiWebhookAdapter> _mockAdapter;
    private readonly Mock<IUserOrchestrationService> _mockUserOrchestrationService;
    private readonly Mock<IAgentSessionService> _mockAgentSessionService;
    private readonly Mock<IMentorshipCacheService> _mockMentorshipCacheService;
    private readonly Mock<IMentorshipRepository> _mockMentorshipRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ZApiWebhookController>> _mockLogger;
    private readonly Mock<ILogSanitizer> _mockLogSanitizer;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly ZApiWebhookController _controller;
    private readonly DefaultHttpContext _httpContext;

    public ZApiWebhookControllerTests()
    {
        _mockMessageProcessor = new Mock<IMessageProcessor>();
        _mockWhatsAppServiceFactory = new Mock<IWhatsAppServiceFactory>();
        _mockAdapter = new Mock<IZApiWebhookAdapter>();
        _mockUserOrchestrationService = new Mock<IUserOrchestrationService>();
        _mockAgentSessionService = new Mock<IAgentSessionService>();
        _mockMentorshipCacheService = new Mock<IMentorshipCacheService>();
        _mockMentorshipRepository = new Mock<IMentorshipRepository>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ZApiWebhookController>>();
        _mockLogSanitizer = new Mock<ILogSanitizer>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _httpContext = new DefaultHttpContext();
        
        // Setup default log sanitizer behavior (return input as-is for tests)
        _mockLogSanitizer.Setup(x => x.MaskPhoneNumber(It.IsAny<string>())).Returns<string?>(s => s ?? "***");
        _mockLogSanitizer.Setup(x => x.MaskToken(It.IsAny<string>())).Returns<string?>(s => s ?? "***");
        _mockLogSanitizer.Setup(x => x.SanitizeMessageText(It.IsAny<string>(), It.IsAny<int>())).Returns<string?, int>((s, _) => s ?? "***");
        _mockLogSanitizer.Setup(x => x.SanitizeJsonPayload(It.IsAny<string>())).Returns<string?>(s => s ?? "{}");

        // Setup environment as Production (not Development) to disable detailed logging in tests
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");
        _mockEnvironment.Setup(x => x.ApplicationName).Returns("Mentoragente.API");
        _mockEnvironment.Setup(x => x.ContentRootPath).Returns(".");
        _mockEnvironment.Setup(x => x.WebRootPath).Returns("wwwroot");

        // Setup service scope factory for background tasks
        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(x => x.GetRequiredService<IWhatsAppServiceFactory>()).Returns(_mockWhatsAppServiceFactory.Object);

        _controller = new ZApiWebhookController(
            _mockMessageProcessor.Object,
            _mockWhatsAppServiceFactory.Object,
            _mockAdapter.Object,
            _mockUserOrchestrationService.Object,
            _mockAgentSessionService.Object,
            _mockMentorshipCacheService.Object,
            _mockMentorshipRepository.Object,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockLogSanitizer.Object,
            _mockEnvironment.Object,
            _mockServiceScopeFactory.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnUnauthorized_WhenZApiTokenHeaderIsMissing()
    {
        // Arrange
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511999999999",
            FromMe = false,
            Type = "text",
            Text = new ZApiTextMessage { Message = "Hello" }
        };

        // Act
        var result = await _controller.ReceiveMessage(webhook, null);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        _mockAdapter.Verify(x => x.Adapt(It.IsAny<object>()), Times.Never);
        _mockMessageProcessor.Verify(x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnUnauthorized_WhenNoMentorshipFoundForInstanceToken()
    {
        // Arrange
        var instanceToken = "test-instance-token";
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511999999999",
            FromMe = false,
            Type = "text",
            Text = new ZApiTextMessage { Message = "Hello" }
        };

        _httpContext.Request.Headers["Z-Api-Token"] = instanceToken;
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByInstanceTokenAsync(instanceToken))
            .ReturnsAsync((Mentorship?)null);

        // Act
        var result = await _controller.ReceiveMessage(webhook, null);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByInstanceTokenAsync(instanceToken), Times.Once);
        _mockAdapter.Verify(x => x.Adapt(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnOk_WhenMessageIsIgnored()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var instanceToken = "test-instance-token";
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            InstanceToken = instanceToken,
            Status = MentorshipStatus.Active 
        };
        
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511999999999",
            FromMe = false,
            Type = "text",
            Text = new ZApiTextMessage { Message = "Hello" }
        };

        _httpContext.Request.Headers["Z-Api-Token"] = instanceToken;
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByInstanceTokenAsync(instanceToken))
            .ReturnsAsync(mentorship);
        _mockAdapter.Setup(x => x.Adapt(webhook))
            .Returns((WhatsAppMessage?)null);

        // Act
        var result = await _controller.ReceiveMessage(webhook, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockMessageProcessor.Verify(x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldProcessMessage_WhenValid()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var instanceToken = "test-instance-token";
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            InstanceToken = instanceToken,
            DurationDays = 30,
            Status = MentorshipStatus.Active
        };
        
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511999999999",
            FromMe = false,
            Type = "text",
            Text = new ZApiTextMessage { Message = "Hello" }
        };

        var genericMessage = new WhatsAppMessage
        {
            PhoneNumber = "5511999999999",
            MessageText = "Hello",
            FromMe = false
        };

        var processingResult = new MessageProcessingResult
        {
            Response = "Response",
            Mentorship = mentorship
        };

        var mockWhatsAppService = new Mock<IWhatsAppService>();

        _httpContext.Request.Headers["Z-Api-Token"] = instanceToken;
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByInstanceTokenAsync(instanceToken))
            .ReturnsAsync(mentorship);
        _mockAdapter.Setup(x => x.Adapt(webhook))
            .Returns(genericMessage);

        _mockMessageProcessor.Setup(x => x.ProcessMessageAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Guid>()))
            .ReturnsAsync(processingResult);

        _mockWhatsAppServiceFactory.Setup(x => x.GetServiceForMentorship(mentorship))
            .Returns(mockWhatsAppService.Object);

        mockWhatsAppService.Setup(x => x.SendMessageAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<Mentorship>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReceiveMessage(webhook, null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByInstanceTokenAsync(instanceToken), Times.Once);
        _mockAdapter.Verify(x => x.Adapt(webhook), Times.Once);
        _mockMessageProcessor.Verify(x => x.ProcessMessageAsync(
            genericMessage.PhoneNumber, 
            genericMessage.MessageText, 
            mentorshipId), Times.Once);
        mockWhatsAppService.Verify(x => x.SendMessageAsync(
            genericMessage.PhoneNumber, 
            processingResult.Response, 
            mentorship), Times.Once);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnBadRequest_WhenMentorshipIdDoesNotMatch()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var differentMentorshipId = Guid.NewGuid();
        var instanceToken = "test-instance-token";
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            InstanceToken = instanceToken,
            Status = MentorshipStatus.Active
        };
        
        var webhook = new ZApiWebhookDto
        {
            Phone = "5511999999999",
            FromMe = false,
            Type = "text",
            Text = new ZApiTextMessage { Message = "Hello" }
        };

        _httpContext.Request.Headers["Z-Api-Token"] = instanceToken;
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByInstanceTokenAsync(instanceToken))
            .ReturnsAsync(mentorship);

        // Act - Pass different mentorshipId that doesn't match the token's mentorship
        var result = await _controller.ReceiveMessage(webhook, differentMentorshipId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByInstanceTokenAsync(instanceToken), Times.Once);
        _mockAdapter.Verify(x => x.Adapt(It.IsAny<object>()), Times.Never);
    }
}


