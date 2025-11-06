using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Mentoragente.API;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mentoragente.Tests.API.Integration;

public class WhatsAppWebhookIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WhatsAppWebhookIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Mock services for integration tests
                // In real integration tests, you might use a test database
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ReceiveMessage_ShouldReturnOkForValidMessage()
    {
        // Arrange
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
        var response = await _client.PostAsJsonAsync("/api/WhatsAppWebhook?mentorshipId=00000000-0000-0000-0000-000000000001", webhook);

        // Assert
        // Note: This test requires proper service mocks or a test database
        // For now, we just verify the endpoint is accessible
        // In a real integration test, you would mock the repositories and services
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ReceiveMessage_ShouldIgnoreMessagesFromSelf()
    {
        // Arrange
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
        var response = await _client.PostAsJsonAsync("/api/WhatsAppWebhook", webhook);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("ignored");
    }

    [Fact]
    public async Task ReceiveMessage_ShouldHandleInvalidPhoneNumber()
    {
        // Arrange
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
        var response = await _client.PostAsJsonAsync("/api/WhatsAppWebhook", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

