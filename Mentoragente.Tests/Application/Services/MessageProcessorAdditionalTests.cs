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
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMentoriaRepository> _mockMentoriaRepository;
    private readonly Mock<IAgentSessionRepository> _mockAgentSessionRepository;
    private readonly Mock<IAgentSessionDataRepository> _mockAgentSessionDataRepository;
    private readonly Mock<IConversationRepository> _mockConversationRepository;
    private readonly Mock<IOpenAIAssistantService> _mockOpenAIAssistantService;
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly MessageProcessor _messageProcessor;

    public MessageProcessorAdditionalTests()
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
    public async Task ProcessMessageAsync_ShouldThrowWhenMentoriaNotFound()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentoriaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var user = new User { Id = userId, PhoneNumber = phoneNumber };

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentoriaRepository.Setup(x => x.GetMentoriaByIdAsync(mentoriaId))
            .ReturnsAsync((Mentoria?)null);

        // Act & Assert
        await _messageProcessor.Invoking(mp => mp.ProcessMessageAsync(phoneNumber, messageText, mentoriaId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mentoria {mentoriaId} not found*");
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldHandleOpenAIServiceException()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentoriaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var agentSessionId = Guid.NewGuid();
        var threadId = "thread_ABC123";
        
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

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentoria.AssistantId))
            .ThrowsAsync(new Exception("OpenAI API error"));

        // Act
        var result = await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("erro");
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "user", messageText), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldUpdateSessionLastInteraction()
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
            Status = AgentSessionStatus.Active,
            TotalMessages = 5
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

        _mockOpenAIAssistantService.Setup(x => x.RunAssistantAsync(threadId, mentoria.AssistantId))
            .ReturnsAsync(responseText);

        // Act
        await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentoriaId);

        // Assert
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(
            It.Is<AgentSession>(s => 
                s.Id == agentSessionId && 
                s.LastInteraction.HasValue &&
                s.TotalMessages == 7)), // 5 + 2 (user + assistant)
            Times.Once);
    }
}

