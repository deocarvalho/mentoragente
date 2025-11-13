using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class MessageProcessorAdditionalTests
{
    private readonly Mock<IUserOrchestrationService> _mockUserOrchestrationService;
    private readonly Mock<IMentorshipCacheService> _mockMentorshipCacheService;
    private readonly Mock<IAgentSessionOrchestrationService> _mockSessionOrchestrationService;
    private readonly Mock<IAccessValidationService> _mockAccessValidationService;
    private readonly Mock<IConversationRepository> _mockConversationRepository;
    private readonly Mock<IOpenAIAssistantService> _mockOpenAIAssistantService;
    private readonly Mock<IWhatsAppServiceFactory> _mockWhatsAppServiceFactory;
    private readonly Mock<ISessionUpdateService> _mockSessionUpdateService;
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly MessageProcessor _messageProcessor;

    public MessageProcessorAdditionalTests()
    {
        _mockUserOrchestrationService = new Mock<IUserOrchestrationService>();
        _mockMentorshipCacheService = new Mock<IMentorshipCacheService>();
        _mockSessionOrchestrationService = new Mock<IAgentSessionOrchestrationService>();
        _mockAccessValidationService = new Mock<IAccessValidationService>();
        _mockConversationRepository = new Mock<IConversationRepository>();
        _mockOpenAIAssistantService = new Mock<IOpenAIAssistantService>();
        _mockWhatsAppServiceFactory = new Mock<IWhatsAppServiceFactory>();
        _mockSessionUpdateService = new Mock<ISessionUpdateService>();
        _mockLogger = new Mock<ILogger<MessageProcessor>>();
        
        _messageProcessor = new MessageProcessor(
            _mockUserOrchestrationService.Object,
            _mockMentorshipCacheService.Object,
            _mockSessionOrchestrationService.Object,
            _mockAccessValidationService.Object,
            _mockConversationRepository.Object,
            _mockOpenAIAssistantService.Object,
            _mockWhatsAppServiceFactory.Object,
            _mockSessionUpdateService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldThrowWhenMentorshipNotFound()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentorshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var user = new User { Id = userId, PhoneNumber = phoneNumber };

        _mockUserOrchestrationService.Setup(x => x.GetOrCreateUserAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipCacheService.Setup(x => x.GetMentorshipAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act & Assert
        await _messageProcessor.Invoking(mp => mp.ProcessMessageAsync(phoneNumber, messageText, mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mentorship {mentorshipId} not found*");
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldHandleOpenAIServiceException()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentorshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        var threadId = "thread_ABC123";
        
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

        _mockSessionOrchestrationService.Setup(x => x.GetOrCreateSessionContextAsync(userId, mentorshipId, mentorship.DurationDays))
            .ReturnsAsync(sessionContext);

        _mockAccessValidationService.Setup(x => x.ValidateAccessAsync(agentSession, sessionData))
            .ReturnsAsync(new AccessValidationResult { IsValid = true });

        _mockOpenAIAssistantService.Setup(x => x.AddUserMessageAsync(threadId, messageText))
            .Returns(Task.CompletedTask);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentorship.AssistantId))
            .ThrowsAsync(new Exception("OpenAI API error"));

        // Act & Assert
        await _messageProcessor.Invoking(mp => mp.ProcessMessageAsync(phoneNumber, messageText, mentorshipId))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*OpenAI API error*");
        
        // Note: When exception occurs, conversation may not be saved depending on when exception is thrown
        _mockOpenAIAssistantService.Verify(x => x.AddUserMessageAsync(threadId, messageText), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldUpdateSessionLastInteraction()
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
            Status = AgentSessionStatus.Active,
            TotalMessages = 5
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

        _mockSessionOrchestrationService.Setup(x => x.GetOrCreateSessionContextAsync(userId, mentorshipId, mentorship.DurationDays))
            .ReturnsAsync(sessionContext);

        _mockAccessValidationService.Setup(x => x.ValidateAccessAsync(agentSession, sessionData))
            .ReturnsAsync(new AccessValidationResult { IsValid = true });

        _mockOpenAIAssistantService.Setup(x => x.AddUserMessageAsync(threadId, messageText))
            .Returns(Task.CompletedTask);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentorship.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        _mockSessionUpdateService.Verify(x => x.UpdateSessionAfterMessageAsync(
            It.Is<AgentSession>(s => s.Id == agentSessionId && s.TotalMessages == 5),
            It.Is<AgentSessionData>(d => d.AgentSessionId == agentSessionId),
            mentorship.DurationDays),
            Times.Once);
    }
}
