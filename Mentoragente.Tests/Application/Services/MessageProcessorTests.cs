using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class MessageProcessorTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMentorshipRepository> _mockMentorshipRepository;
    private readonly Mock<IAgentSessionRepository> _mockAgentSessionRepository;
    private readonly Mock<IAgentSessionDataRepository> _mockAgentSessionDataRepository;
    private readonly Mock<IConversationRepository> _mockConversationRepository;
    private readonly Mock<IOpenAIAssistantService> _mockOpenAIAssistantService;
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly MessageProcessor _messageProcessor;

    public MessageProcessorTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMentorshipRepository = new Mock<IMentorshipRepository>();
        _mockAgentSessionRepository = new Mock<IAgentSessionRepository>();
        _mockAgentSessionDataRepository = new Mock<IAgentSessionDataRepository>();
        _mockConversationRepository = new Mock<IConversationRepository>();
        _mockOpenAIAssistantService = new Mock<IOpenAIAssistantService>();
        _mockLogger = new Mock<ILogger<MessageProcessor>>();
        
        _messageProcessor = new MessageProcessor(
            _mockUserRepository.Object,
            _mockMentorshipRepository.Object,
            _mockAgentSessionRepository.Object,
            _mockAgentSessionDataRepository.Object,
            _mockConversationRepository.Object,
            _mockOpenAIAssistantService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldHandleEmptyMessage()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "";
        var mentorshipId = Guid.NewGuid();

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("message");
        _mockUserRepository.Verify(x => x.GetUserByPhoneAsync(It.IsAny<string>()), Times.Never);
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

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync((User?)null);

        _mockUserRepository.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => 
            {
                user.Id = userId;
                return user;
            });

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionWithDataAsync(userId, mentorshipId))
            .ReturnsAsync((AgentSessionWithData?)null);

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionWithDataAsync(userId, mentorshipId))
            .ReturnsAsync((AgentSessionWithData?)null);

        var agentSessionId = Guid.NewGuid();
        _mockAgentSessionRepository.Setup(x => x.CreateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession session) => 
            {
                session.Id = agentSessionId;
                return session;
            });

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession session) => session);

        _mockAgentSessionDataRepository.Setup(x => x.CreateAgentSessionDataAsync(It.IsAny<AgentSessionData>()))
            .ReturnsAsync((AgentSessionData data) => data);

        _mockAgentSessionDataRepository.Setup(x => x.UpdateAgentSessionDataAsync(It.IsAny<AgentSessionData>()))
            .ReturnsAsync((AgentSessionData data) => data);

        _mockOpenAIAssistantService.Setup(x => x.CreateThreadAsync())
            .ReturnsAsync(threadId);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentorship.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().Be(responseText);
        _mockUserRepository.Verify(x => x.CreateUserAsync(It.IsAny<User>()), Times.Once);
        _mockAgentSessionRepository.Verify(x => x.CreateAgentSessionAsync(It.IsAny<AgentSession>()), Times.Once);
        _mockAgentSessionDataRepository.Verify(x => x.CreateAgentSessionDataAsync(It.IsAny<AgentSessionData>()), Times.Once);
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

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Active session returns null (session is expired, so not active)
        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionWithDataAsync(userId, mentorshipId))
            .ReturnsAsync((AgentSessionWithData?)null);

        // But there's an existing expired session
        var expiredSessionWithData = new AgentSessionWithData
        {
            Session = agentSession,
            Data = sessionData
        };

        _mockAgentSessionRepository.Setup(x => x.GetAgentSessionWithDataAsync(userId, mentorshipId))
            .ReturnsAsync(expiredSessionWithData);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().Contain("access period");
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(It.Is<AgentSession>(s => s.Status == AgentSessionStatus.Expired)), Times.Once);
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

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        var sessionWithData = new AgentSessionWithData
        {
            Session = agentSession,
            Data = sessionData
        };

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionWithDataAsync(userId, mentorshipId))
            .ReturnsAsync(sessionWithData);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        _mockAgentSessionDataRepository.Setup(x => x.UpdateAgentSessionDataAsync(It.IsAny<AgentSessionData>()))
            .ReturnsAsync((AgentSessionData data) => data);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentorship.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().Be(responseText);
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "user", messageText), Times.Once);
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "assistant", responseText), Times.Once);
        _mockOpenAIAssistantService.Verify(x => x.AddUserMessageAsync(threadId, messageText), Times.Once);
        _mockOpenAIAssistantService.Verify(x => x.RunAssistantAsync(threadId, mentorship.AssistantId), Times.Once);
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

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        var sessionWithData = new AgentSessionWithData
        {
            Session = agentSession,
            Data = sessionData
        };

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionWithDataAsync(userId, mentorshipId))
            .ReturnsAsync(sessionWithData);

        _mockAgentSessionRepository.Setup(x => x.UpdateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession s) => s);

        _mockAgentSessionDataRepository.Setup(x => x.UpdateAgentSessionDataAsync(It.IsAny<AgentSessionData>()))
            .ReturnsAsync((AgentSessionData data) => data);

        _mockOpenAIAssistantService.Setup(x => x.CreateThreadAsync())
            .ReturnsAsync(newThreadId);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(newThreadId, mentorship.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        result.Should().Be(responseText);
        _mockOpenAIAssistantService.Verify(x => x.CreateThreadAsync(), Times.Once);
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(
            It.Is<AgentSession>(s => s.AIContextId == newThreadId)), Times.AtLeastOnce);
    }
}

