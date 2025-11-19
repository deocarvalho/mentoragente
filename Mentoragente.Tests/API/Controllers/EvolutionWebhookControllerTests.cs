using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Mentoragente.API.Controllers;
using Mentoragente.Application.Adapters;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Tests.API.Controllers;

public class EvolutionWebhookControllerTests
{
    private readonly Mock<IMessageProcessor> _mockMessageProcessor;
    private readonly Mock<IWhatsAppServiceFactory> _mockWhatsAppServiceFactory;
    private readonly Mock<IEvolutionWebhookAdapter> _mockAdapter;
    private readonly Mock<IUserOrchestrationService> _mockUserOrchestrationService;
    private readonly Mock<IAgentSessionService> _mockAgentSessionService;
    private readonly Mock<IMentorshipCacheService> _mockMentorshipCacheService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EvolutionWebhookController>> _mockLogger;
    private readonly Mock<ILogSanitizer> _mockLogSanitizer;
    private readonly EvolutionWebhookController _controller;

    public EvolutionWebhookControllerTests()
    {
        _mockMessageProcessor = new Mock<IMessageProcessor>();
        _mockWhatsAppServiceFactory = new Mock<IWhatsAppServiceFactory>();
        _mockAdapter = new Mock<IEvolutionWebhookAdapter>();
        _mockUserOrchestrationService = new Mock<IUserOrchestrationService>();
        _mockAgentSessionService = new Mock<IAgentSessionService>();
        _mockMentorshipCacheService = new Mock<IMentorshipCacheService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EvolutionWebhookController>>();
        _mockLogSanitizer = new Mock<ILogSanitizer>();
        
        // Setup default log sanitizer behavior (return input as-is for tests)
        _mockLogSanitizer.Setup(x => x.MaskPhoneNumber(It.IsAny<string>())).Returns<string?>(s => s ?? "***");
        _mockLogSanitizer.Setup(x => x.MaskToken(It.IsAny<string>())).Returns<string?>(s => s ?? "***");
        _mockLogSanitizer.Setup(x => x.SanitizeMessageText(It.IsAny<string>(), It.IsAny<int>())).Returns<string?, int>((s, _) => s ?? "***");
        _mockLogSanitizer.Setup(x => x.SanitizeJsonPayload(It.IsAny<string>())).Returns<string?>(s => s ?? "{}");

        _controller = new EvolutionWebhookController(
            _mockMessageProcessor.Object,
            _mockWhatsAppServiceFactory.Object,
            _mockAdapter.Object,
            _mockUserOrchestrationService.Object,
            _mockAgentSessionService.Object,
            _mockMentorshipCacheService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object,
            _mockLogSanitizer.Object);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnBadRequest_WhenMentorshipIdIsEmpty()
    {
        // Arrange
        var webhook = new EvolutionWebhookDto
        {
            Event = "messages.upsert",
            Data = new EvolutionWebhookData
            {
                Key = new EvolutionWebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = false
                },
                Message = new EvolutionWebhookMessage { Conversation = "Hello" }
            }
        };

        // Act
        var result = await _controller.ReceiveMessage(webhook, Guid.Empty);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockAdapter.Verify(x => x.Adapt(It.IsAny<object>()), Times.Never);
        _mockMessageProcessor.Verify(x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnOk_WhenMessageIsIgnored()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var webhook = new EvolutionWebhookDto
        {
            Event = "messages.upsert",
            Data = new EvolutionWebhookData
            {
                Key = new EvolutionWebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = false
                },
                Message = new EvolutionWebhookMessage { Conversation = "Hello" }
            }
        };

        _mockAdapter.Setup(x => x.Adapt(webhook))
            .Returns((WhatsAppMessage?)null);

        // Act
        var result = await _controller.ReceiveMessage(webhook, mentorshipId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockMessageProcessor.Verify(x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldProcessMessage_WhenValid()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var webhook = new EvolutionWebhookDto
        {
            Event = "messages.upsert",
            Data = new EvolutionWebhookData
            {
                Key = new EvolutionWebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = false
                },
                Message = new EvolutionWebhookMessage { Conversation = "Hello" }
            }
        };

        var genericMessage = new WhatsAppMessage
        {
            PhoneNumber = "5511999999999",
            MessageText = "Hello",
            FromMe = false
        };

        var mentorship = new Mentorship { Id = mentorshipId, DurationDays = 30 };
        var processingResult = new MessageProcessingResult
        {
            Response = "Response",
            Mentorship = mentorship
        };

        var mockWhatsAppService = new Mock<IWhatsAppService>();

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
        var result = await _controller.ReceiveMessage(webhook, mentorshipId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
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
    public async Task ReceiveMessage_ShouldReturnBadRequest_WhenSendFails()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var webhook = new EvolutionWebhookDto
        {
            Event = "messages.upsert",
            Data = new EvolutionWebhookData
            {
                Key = new EvolutionWebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = false
                },
                Message = new EvolutionWebhookMessage { Conversation = "Hello" }
            }
        };

        var genericMessage = new WhatsAppMessage
        {
            PhoneNumber = "5511999999999",
            MessageText = "Hello",
            FromMe = false
        };

        var mentorship = new Mentorship { Id = mentorshipId, DurationDays = 30 };
        var processingResult = new MessageProcessingResult
        {
            Response = "Response",
            Mentorship = mentorship
        };

        var mockWhatsAppService = new Mock<IWhatsAppService>();

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
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReceiveMessage(webhook, mentorshipId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        mockWhatsAppService.Verify(x => x.SendMessageAsync(
            genericMessage.PhoneNumber, 
            processingResult.Response, 
            mentorship), Times.Once);
    }
}


