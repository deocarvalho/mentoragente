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

public class MessageProcessorAdditionalTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMentorshipRepository> _mockMentorshipRepository;
    private readonly Mock<IAgentSessionRepository> _mockAgentSessionRepository;
    private readonly Mock<IAgentSessionDataRepository> _mockAgentSessionDataRepository;
    private readonly Mock<IConversationRepository> _mockConversationRepository;
    private readonly Mock<IOpenAIAssistantService> _mockOpenAIAssistantService;
    private readonly Mock<ILogger<MessageProcessor>> _mockLogger;
    private readonly MessageProcessor _messageProcessor;

    public MessageProcessorAdditionalTests()
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
    public async Task ProcessMessageAsync_ShouldThrowWhenMentorshipNotFound()
    {
        // Arrange
        var phoneNumber = "5511999999999";
        var messageText = "Hello!";
        var mentorshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var user = new User { Id = userId, PhoneNumber = phoneNumber };

        _mockUserRepository.Setup(x => x.GetUserByPhoneAsync(phoneNumber))
            .ReturnsAsync(user);

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
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
            .ThrowsAsync(new Exception("OpenAI API error"));

        // Act & Assert
        await _messageProcessor.Invoking(mp => mp.ProcessMessageAsync(phoneNumber, messageText, mentorshipId))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*OpenAI API error*");
        
        _mockConversationRepository.Verify(x => x.AddMessageAsync(agentSessionId, "user", messageText), Times.Once);
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
        await _messageProcessor.ProcessMessageAsync(phoneNumber, messageText, mentorshipId);

        // Assert
        _mockAgentSessionRepository.Verify(x => x.UpdateAgentSessionAsync(
            It.Is<AgentSession>(s => 
                s.Id == agentSessionId && 
                s.LastInteraction.HasValue &&
                s.TotalMessages == 7)), // 5 + 2 (user + assistant)
            Times.Once);
        
        _mockAgentSessionDataRepository.Verify(x => x.UpdateAgentSessionDataAsync(It.IsAny<AgentSessionData>()), Times.Once);
    }
}

