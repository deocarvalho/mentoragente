using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Mentoragente.API;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;

namespace Mentoragente.Tests.API.Integration;

public class WhatsAppWebhookIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly IntegrationTestHelper _helper;
    private readonly HttpClient _client;

    public WhatsAppWebhookIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _helper = new IntegrationTestHelper();
        _client = _helper.Client;
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnOkForValidMessage()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            Name = "Test Mentorship",
            Status = MentorshipStatus.Active
        };

        var processingResult = new MessageProcessingResult
        {
            Response = "Hello! How can I help you?",
            Mentorship = mentorship
        };

        _helper.MockMentorshipService
            .Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _helper.MockMessageProcessor
            .Setup(x => x.ProcessMessageAsync("5511999999999", "Hello!", mentorshipId))
            .ReturnsAsync(processingResult);

        _helper.MockEvolutionAPIService
            .Setup(x => x.SendMessageAsync("5511999999999", processingResult.Response, mentorship))
            .ReturnsAsync(true);

        var webhook = new WhatsAppWebhookDto
        {
            Event = "messages.upsert",
            Data = new WebhookData
            {
                Key = new WebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = false
                },
                Message = new WebhookMessage { Conversation = "Hello!" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/WhatsAppWebhook?mentorshipId={mentorshipId}", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("success");
        
        _helper.MockMessageProcessor.Verify(
            x => x.ProcessMessageAsync("5511999999999", "Hello!", mentorshipId),
            Times.Once);
        _helper.MockEvolutionAPIService.Verify(
            x => x.SendMessageAsync("5511999999999", processingResult.Response, mentorship),
            Times.Once);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnBadRequest_WhenMentorshipIdIsEmpty()
    {
        // Arrange
        var mentorshipId = Guid.Empty;
        var webhook = new WhatsAppWebhookDto
        {
            Event = "messages.upsert",
            Data = new WebhookData
            {
                Key = new WebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = false
                },
                Message = new WebhookMessage { Conversation = "Hello!" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/WhatsAppWebhook?mentorshipId={mentorshipId}", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("MentorshipId");
        
        // Verify that message processor was NOT called
        _helper.MockMessageProcessor.Verify(
            x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldIgnoreMessagesFromSelf()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var webhook = new WhatsAppWebhookDto
        {
            Event = "messages.upsert",
            Data = new WebhookData
            {
                Key = new WebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = true
                },
                Message = new WebhookMessage { Conversation = "Hello!" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/WhatsAppWebhook?mentorshipId={mentorshipId}", webhook);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("ignored");
        
        // Verify that message processor was NOT called
        _helper.MockMessageProcessor.Verify(
            x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldHandleInvalidPhoneNumber()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var webhook = new WhatsAppWebhookDto
        {
            Event = "messages.upsert",
            Data = new WebhookData
            {
                Key = new WebhookKey
                {
                    RemoteJid = "invalid@s.whatsapp.net",
                    FromMe = false
                },
                Message = new WebhookMessage { Conversation = "Hello!" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/WhatsAppWebhook?mentorshipId={mentorshipId}", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ignored");
        
        // Verify that message processor was NOT called
        _helper.MockMessageProcessor.Verify(
            x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnBadRequestWhenMessageSendingFails()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship
        {
            Id = mentorshipId,
            Name = "Test Mentorship",
            Status = MentorshipStatus.Active
        };

        var processingResult = new MessageProcessingResult
        {
            Response = "Hello! How can I help you?",
            Mentorship = mentorship
        };

        _helper.MockMentorshipService
            .Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _helper.MockMessageProcessor
            .Setup(x => x.ProcessMessageAsync("5511999999999", "Hello!", mentorshipId))
            .ReturnsAsync(processingResult);

        _helper.MockEvolutionAPIService
            .Setup(x => x.SendMessageAsync("5511999999999", processingResult.Response, mentorship))
            .ReturnsAsync(false); // Simulate failure

        var webhook = new WhatsAppWebhookDto
        {
            Event = "messages.upsert",
            Data = new WebhookData
            {
                Key = new WebhookKey
                {
                    RemoteJid = "5511999999999@s.whatsapp.net",
                    FromMe = false
                },
                Message = new WebhookMessage { Conversation = "Hello!" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/WhatsAppWebhook?mentorshipId={mentorshipId}", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Failed to send response");
    }

    public void Dispose()
    {
        _helper?.Dispose();
        _client?.Dispose();
    }
}

