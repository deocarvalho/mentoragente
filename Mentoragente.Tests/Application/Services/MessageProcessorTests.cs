using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;

namespace Mentoragente.Tests.Application.Services;

public class MessageProcessorTests
{
    private readonly Mock<IUserOrchestrationService> _mockUserOrchestrationService;
    private readonly Mock<IMentorshipCacheService> _mockMentorshipCacheService;
    private readonly Mock<IAgentSessionOrchestrationService> _mockSessionOrchestrationService;
    private readonly Mock<IAccessValidationService> _mockAccessValidationService;
    private readonly Mock<IConversationRepository> _mockConversationRepository;
    private readonly Mock<IOpenAIAssistantService> _mockOpenAIAssistantService;
    private readonly Mock<IWhatsAppServiceFactory> _mockWhatsAppServiceFactory;
    private readonly Mock<ISessionUpdateService> _mockSessionUpdateService;
    private readonly Mock<IPhoneNumberValidator> _mockPhoneNumberValidator;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly MessageProcessor _messageProcessor;

    public MessageProcessorTests()
    {
        _mockUserOrchestrationService = new Mock<IUserOrchestrationService>();
        _mockMentorshipCacheService = new Mock<IMentorshipCacheService>();
        _mockSessionOrchestrationService = new Mock<IAgentSessionOrchestrationService>();
        _mockAccessValidationService = new Mock<IAccessValidationService>();
        _mockConversationRepository = new Mock<IConversationRepository>();
        _mockOpenAIAssistantService = new Mock<IOpenAIAssistantService>();
        _mockWhatsAppServiceFactory = new Mock<IWhatsAppServiceFactory>();
        _mockSessionUpdateService = new Mock<ISessionUpdateService>();
        _mockPhoneNumberValidator = new Mock<IPhoneNumberValidator>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<MessageProcessor>>();
        
        // Setup default mocks
        _mockPhoneNumberValidator.Setup(x => x.IsValidPhoneNumber(It.IsAny<string>())).Returns(true);
        _mockInputSanitizer.Setup(x => x.SanitizeMessage(It.IsAny<string>())).Returns<string>(s => s);
        
        _messageProcessor = new MessageProcessor(
            _mockUserOrchestrationService.Object,
            _mockMentorshipCacheService.Object,
            _mockSessionOrchestrationService.Object,
            _mockAccessValidationService.Object,
            _mockConversationRepository.Object,
            _mockOpenAIAssistantService.Object,
            _mockWhatsAppServiceFactory.Object,
            _mockSessionUpdateService.Object,
            _mockPhoneNumberValidator.Object,
            _mockInputSanitizer.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldHandleEmptyMessage()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "";
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship { Id = mentorshipId, DurationDays = 30 };

        _mockMentorshipCacheService.Setup(x => x.GetMentorshipAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Contain("message");
        result.Mentorship.Should().Be(mentorship);
        _mockUserOrchestrationService.Verify(x => x.GetOrCreateUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldCreateUserIfNotExists()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentorshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            Name = "Test Mentorship",
            AssistantId = "asst_TEST",
            DurationDays = 30,
            Status = MentorshipStatus.Active
        };
        var threadId = "thread_ABC123";
        var responseText = "Hi there!";
        var agentSessionId = Guid.NewGuid();

        var user = new User { Id = userId, PhoneNumber = phoneNumber };
        var agentSession = new AgentSession 
        { 
            Id = agentSessionId, 
            UserId = userId, 
            MentorshipId = mentorshipId,
            AIContextId = threadId,
            Status = AgentSessionStatus.Active 
        };
        var sessionData = new AgentSessionData 
        { 
            AgentSessionId = agentSessionId, 
            AccessEndDate = DateTime.UtcNow.AddDays(10) 
        };
        var sessionContext = new AgentSessionContext
        {
            Session = agentSession,
            Data = sessionData
        };

        _mockUserOrchestrationService.Setup(x => x.GetOrCreateUserAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipCacheService.Setup(x => x.GetMentorshipAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockSessionOrchestrationService.Setup(x => x.GetSessionContextAsync(userId, mentorshipId))
            .ReturnsAsync(sessionContext);

        _mockAccessValidationService.Setup(x => x.ValidateAccessAsync(agentSession, sessionData))
            .ReturnsAsync(new AccessValidationResult { IsValid = true });

        _mockOpenAIAssistantService.Setup(x => x.AddUserMessageAsync(threadId, messageText))
            .Returns(Task.CompletedTask);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentorship.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Be(responseText);
        result.Mentorship.Should().Be(mentorship);
        _mockUserOrchestrationService.Verify(x => x.GetOrCreateUserAsync(phoneNumber), Times.Once);
        _mockSessionUpdateService.Verify(x => x.UpdateSessionAfterMessageAsync(agentSession, sessionData, mentorship.DurationDays), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldRejectExpiredAccess()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentorshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        
        var user = new User { Id = userId, PhoneNumber = phoneNumber };
        var mentorship = new Mentorship { Id = mentorshipId, DurationDays = 30 };
        var agentSession = new AgentSession { Id = agentSessionId, UserId = userId, MentorshipId = mentorshipId, Status = AgentSessionStatus.Active };
        var sessionData = new AgentSessionData 
        { 
            AgentSessionId = agentSessionId, 
            AccessEndDate = DateTime.UtcNow.AddDays(-1) // Expired
        };
        var sessionContext = new AgentSessionContext
        {
            Session = agentSession,
            Data = sessionData
        };

        _mockUserOrchestrationService.Setup(x => x.GetOrCreateUserAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipCacheService.Setup(x => x.GetMentorshipAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockSessionOrchestrationService.Setup(x => x.GetSessionContextAsync(userId, mentorshipId))
            .ReturnsAsync(sessionContext);

        _mockAccessValidationService.Setup(x => x.ValidateAccessAsync(agentSession, sessionData))
            .ReturnsAsync(new AccessValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = "Your access period to this mentorship has ended. Please contact to renew." 
            });

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Contain("access period");
        result.Mentorship.Should().Be(mentorship);
        _mockOpenAIAssistantService.Verify(x => x.RunAssistantAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldProcessValidMessage()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentorshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        var threadId = "thread_ABC123";
        var responseText = "Hi there!";
        
        var user = new User { Id = userId, PhoneNumber = phoneNumber };
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            AssistantId = "asst_TEST",
            DurationDays = 30,
            Status = MentorshipStatus.Active
        };
        var agentSession = new AgentSession 
        { 
            Id = agentSessionId, 
            UserId = userId, 
            MentorshipId = mentorshipId,
            AIContextId = threadId,
            Status = AgentSessionStatus.Active 
        };
        var sessionData = new AgentSessionData 
        { 
            AgentSessionId = agentSessionId, 
            AccessEndDate = DateTime.UtcNow.AddDays(10) // Still valid
        };
        var sessionContext = new AgentSessionContext
        {
            Session = agentSession,
            Data = sessionData
        };

        _mockUserOrchestrationService.Setup(x => x.GetOrCreateUserAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipCacheService.Setup(x => x.GetMentorshipAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockSessionOrchestrationService.Setup(x => x.GetSessionContextAsync(userId, mentorshipId))
            .ReturnsAsync(sessionContext);

        _mockAccessValidationService.Setup(x => x.ValidateAccessAsync(agentSession, sessionData))
            .ReturnsAsync(new AccessValidationResult { IsValid = true });

        _mockOpenAIAssistantService.Setup(x => x.AddUserMessageAsync(threadId, messageText))
            .Returns(Task.CompletedTask);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentorship.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Be(responseText);
        result.Mentorship.Should().Be(mentorship);
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "user", messageText), Times.Once);
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "assistant", responseText), Times.Once);
        _mockOpenAIAssistantService.Verify(x => x.AddUserMessageAsync(threadId, messageText), Times.Once);
        _mockOpenAIAssistantService.Verify(x => x.RunAssistantAsync(threadId, mentorship.AssistantId), Times.Once);
        _mockSessionUpdateService.Verify(x => x.UpdateSessionAfterMessageAsync(agentSession, sessionData, mentorship.DurationDays), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldCreateThreadIdIfMissing()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentorshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        var newThreadId = "thread_NEW123";
        var responseText = "Hi there!";
        
        var user = new User { Id = userId };
        var mentorship = new Mentorship { Id = mentorshipId, AssistantId = "asst_TEST", DurationDays = 30 };
        var agentSession = new AgentSession 
        { 
            Id = agentSessionId, 
            UserId = userId, 
            MentorshipId = mentorshipId,
            AIContextId = null // No thread ID yet
        };
        var sessionData = new AgentSessionData 
        { 
            AgentSessionId = agentSessionId, 
            AccessEndDate = DateTime.UtcNow.AddDays(10) 
        };
        var sessionContext = new AgentSessionContext
        {
            Session = agentSession,
            Data = sessionData
        };

        _mockUserOrchestrationService.Setup(x => x.GetOrCreateUserAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipCacheService.Setup(x => x.GetMentorshipAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockSessionOrchestrationService.Setup(x => x.GetSessionContextAsync(userId, mentorshipId))
            .ReturnsAsync(sessionContext);

        _mockAccessValidationService.Setup(x => x.ValidateAccessAsync(agentSession, sessionData))
            .ReturnsAsync(new AccessValidationResult { IsValid = true });

        _mockSessionOrchestrationService.Setup(x => x.EnsureThreadExistsAsync(agentSession))
            .Callback<AgentSession>(s => s.AIContextId = newThreadId)
            .Returns(Task.CompletedTask);

        _mockOpenAIAssistantService.Setup(x => x.AddUserMessageAsync(newThreadId, messageText))
            .Returns(Task.CompletedTask);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(newThreadId, mentorship.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Be(responseText);
        _mockSessionOrchestrationService.Verify(x => x.EnsureThreadExistsAsync(agentSession), Times.Once);
    }
}
