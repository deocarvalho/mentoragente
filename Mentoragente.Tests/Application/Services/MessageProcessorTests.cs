using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class MessageProcessorTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMentoriaRepository> _mockMentoriaRepository;
    private readonly Mock<IAgentSessionRepository> _mockAgentSessionRepository;
    private readonly Mock<IAgentSessionDataRepository> _mockAgentSessionDataRepository;
    private readonly Mock<IConversationRepository> _mockConversationRepository;
    private readonly Mock<IOpenAIAssistantService> _mockOpenAIAssistantService;
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly MessageProcessor _messageProcessor;

    public MessageProcessorTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMentoriaRepository = new Mock<IMentoriaRepository>();
        _mockAgentSessionRepository = new Mock<IAgentSessionRepository>();
        _mockAgentSessionDataRepository = new Mock<IAgentSessionDataRepository>();
        _mockConversationRepository = new Mock<IConversationRepository>();
        _mockOpenAIAssistantService = new Mock<IOpenAIAssistantService>();
        _mockLogger = new Mock<ILogger<MessageProcessor>>();
        
        _messageProcessor = new MessageProcessor(
            _mockUserRepository.Object,
            _mockMentoriaRepository.Object,
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
        var mentoriaId = Guid.NewGuid();

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("mensagem");
        _mockUserRepository.Verify(x => x.GetUserByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldCreateUserIfNotExists()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentoriaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mentoria = new Mentoria
        {
            Id = mentoriaId,
            Nome = "Test Mentoria",
            AssistantId = "asst_TEST",
            DuracaoDias = 30,
            Status = MentoriaStatus.Active
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

        _mockMentoriaRepository.Setup(x => x.GetMentoriaByIdAsync(mentoriaId))
            .ReturnsAsync(mentoria);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentoriaId))
            .ReturnsAsync((AgentSession?)null);

        _mockAgentSessionRepository.Setup(x => x.CreateAgentSessionAsync(It.IsAny<AgentSession>()))
            .ReturnsAsync((AgentSession session) => session);

        _mockAgentSessionDataRepository.Setup(x => x.CreateAgentSessionDataAsync(It.IsAny<AgentSessionData>()))
            .ReturnsAsync((AgentSessionData data) => data);

        _mockOpenAIAssistantService.Setup(x => x.CreateThreadAsync())
            .ReturnsAsync(threadId);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentoria.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

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
        var mentoriaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        
        var user = new User { Id = userId, PhoneNumber = phoneNumber };
        var mentoria = new Mentoria { Id = mentoriaId, DuracaoDias = 30 };
        var agentSession = new AgentSession { Id = agentSessionId, UserId = userId, MentoriaId = mentoriaId, Status = AgentSessionStatus.Active };
        var sessionData = new AgentSessionData 
        { 
            AgentSessionId = agentSessionId, 
            AccessEndDate = DateTime.UtcNow.AddDays(-1) // Expired
        };

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentoriaRepository.Setup(x => x.GetMentoriaByIdAsync(mentoriaId))
            .ReturnsAsync(mentoria);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentoriaId))
            .ReturnsAsync(agentSession);

        _mockAgentSessionDataRepository.Setup(x => x.GetAgentSessionDataAsync(agentSessionId))
            .ReturnsAsync(sessionData);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

        // Assert
        result.Should().Contain("período de acesso terminou");
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(It.Is<AgentSession>(s => s.Status == AgentSessionStatus.Expired)), Times.Once);
        _mockOpenAIAssistantService.Verify(x => x.RunAssistantAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldProcessValidMessage()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentoriaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        var threadId = "thread_ABC123";
        var responseText = "Hi there!";
        
        var user = new User { Id = userId, PhoneNumber = phoneNumber };
        var mentoria = new Mentoria 
        { 
            Id = mentoriaId, 
            AssistantId = "asst_TEST",
            DuracaoDias = 30,
            Status = MentoriaStatus.Active
        };
        var agentSession = new AgentSession 
        { 
            Id = agentSessionId, 
            UserId = userId, 
            MentoriaId = mentoriaId,
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

        _mockMentoriaRepository.Setup(x => x.GetMentoriaByIdAsync(mentoriaId))
            .ReturnsAsync(mentoria);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentoriaId))
            .ReturnsAsync(agentSession);

        _mockAgentSessionDataRepository.Setup(x => x.GetAgentSessionDataAsync(agentSessionId))
            .ReturnsAsync(sessionData);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentoria.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

        // Assert
        result.Should().Be(responseText);
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "user", messageText), Times.Once);
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "assistant", responseText), Times.Once);
        _mockOpenAIAssistantService.Verify(x => x.AddUserMessageAsync(threadId, messageText), Times.Once);
        _mockOpenAIAssistantService.Verify(x => x.RunAssistantAsync(threadId, mentoria.AssistantId), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldCreateThreadIdIfMissing()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentoriaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        var newThreadId = "thread_NEW123";
        var responseText = "Hi there!";
        
        var user = new User { Id = userId };
        var mentoria = new Mentoria { Id = mentoriaId, AssistantId = "asst_TEST", DuracaoDias = 30 };
        var agentSession = new AgentSession 
        { 
            Id = agentSessionId, 
            UserId = userId, 
            MentoriaId = mentoriaId,
            AIContextId = null // No thread ID yet
        };
        var sessionData = new AgentSessionData 
        { 
            AgentSessionId = agentSessionId, 
            AccessEndDate = DateTime.UtcNow.AddDays(10) 
        };

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentoriaRepository.Setup(x => x.GetMentoriaByIdAsync(mentoriaId))
            .ReturnsAsync(mentoria);

        _mockAgentSessionRepository.Setup(x => x.GetActiveAgentSessionAsync(userId, mentoriaId))
            .ReturnsAsync(agentSession);

        _mockAgentSessionDataRepository.Setup(x => x.GetAgentSessionDataAsync(agentSessionId))
            .ReturnsAsync(sessionData);

        _mockOpenAIAssistantService.Setup(x => x.CreateThreadAsync())
            .ReturnsAsync(newThreadId);

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(newThreadId, mentoria.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

        // Assert
        result.Should().Be(responseText);
        _mockOpenAIAssistantService.Verify(x => x.CreateThreadAsync(), Times.Once);
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(
            It.Is<AgentSession>(s => s.AIContextId == newThreadId)), Times.Once);
    }
}

