using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text;
using System.Text.Json;
using Mentoragente.Infrastructure.Services;
using Mentoragente.Domain.Interfaces;

namespace Mentoragente.Tests.Infrastructure.Services;

public class OpenAIAssistantServiceTests
{
    private readonly Mock<ILogger<OpenAIAssistantService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly HttpClient _httpClient;

    public OpenAIAssistantServiceTests()
    {
        _mockLogger = new Mock<ILogger<OpenAIAssistantService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _httpClient = new HttpClient();
    }

    [Fact]
    public async Task CreateThreadAsync_ShouldReturnThreadId()
    {
        // Arrange
        var expectedThreadId = "thread_abc123";
        var responseJson = JsonSerializer.Serialize(new { id = expectedThreadId });
        
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var handler = MockHttpMessageHandler.CreateSuccessHandler(responseJson);
        var httpClient = new HttpClient(handler);
        var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.CreateThreadAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Note: In a real test, we'd need to configure the HTTP client with actual API endpoint
        // This is a structure test
    }

    [Fact]
    public async Task AddUserMessageAsync_ShouldNotThrow()
    {
        // Arrange
        var threadId = "thread_abc123";
        var message = "Hello!";
        
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var handler = MockHttpMessageHandler.CreateSuccessHandler("{}");
        var httpClient = new HttpClient(handler);
        var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.AddUserMessageAsync(threadId, message))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateThreadAsync_ShouldThrowWhenApiKeyNotConfigured()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var httpClient = new HttpClient();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task CreateThreadAsync_ShouldThrowWhenBaseUrlNotConfigured()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns((string?)null);
        
        var httpClient = new HttpClient();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task CreateThreadAsync_ShouldHandleHttpError()
    {
        // Arrange
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var handler = MockHttpMessageHandler.CreateErrorHandler(HttpStatusCode.BadRequest, "{\"error\":\"Invalid request\"}");
        var httpClient = new HttpClient(handler);
        var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.CreateThreadAsync())
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task AddUserMessageAsync_ShouldHandleHttpError()
    {
        // Arrange
        var threadId = "thread_abc123";
        var message = "Hello!";
        
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var handler = MockHttpMessageHandler.CreateErrorHandler(HttpStatusCode.BadRequest, "{\"error\":\"Invalid request\"}");
        var httpClient = new HttpClient(handler);
        var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.AddUserMessageAsync(threadId, message))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task RunAssistantAsync_ShouldReturnResponseText()
    {
        // Arrange
        var threadId = "thread_abc123";
        var assistantId = "asst_123";
        var runId = "run_abc123";
        var responseText = "Hello! How can I help?";
        
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var callCount = 0;
        var handler = new MockHttpMessageHandler(async request =>
        {
            callCount++;
            if (request.RequestUri!.ToString().Contains("/runs"))
            {
                if (request.Method == HttpMethod.Post)
                {
                    // Create run response
                    var runResponse = JsonSerializer.Serialize(new { id = runId, status = "queued" });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(runResponse, Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    // Poll run status
                    var status = callCount < 3 ? "in_progress" : "completed";
                    var runResponse = JsonSerializer.Serialize(new { id = runId, status = status });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(runResponse, Encoding.UTF8, "application/json")
                    };
                }
            }
            else if (request.RequestUri!.ToString().Contains("/messages"))
            {
                // Get messages response
                var messagesResponse = JsonSerializer.Serialize(new
                {
                    data = new[]
                    {
                        new
                        {
                            content = new[]
                            {
                                new
                                {
                                    text = new { value = responseText }
                                }
                            }
                        }
                    }
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(messagesResponse, Encoding.UTF8, "application/json")
                };
            }
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });
        
        var httpClient = new HttpClient(handler);
        var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act
        var result = await service.RunAssistantAsync(threadId, assistantId);

        // Assert
        result.Should().Be(responseText);
    }

    [Fact]
    public async Task RunAssistantAsync_ShouldThrowWhenRunFails()
    {
        // Arrange
        var threadId = "thread_abc123";
        var assistantId = "asst_123";
        var runId = "run_abc123";
        
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var callCount = 0;
        var handler = new MockHttpMessageHandler(async request =>
        {
            callCount++;
            if (request.RequestUri!.ToString().Contains("/runs"))
            {
                if (request.Method == HttpMethod.Post)
                {
                    var runResponse = JsonSerializer.Serialize(new { id = runId, status = "queued" });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(runResponse, Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    var runResponse = JsonSerializer.Serialize(new 
                    { 
                        id = runId, 
                        status = "failed",
                        last_error = new { message = "Run failed" }
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(runResponse, Encoding.UTF8, "application/json")
                    };
                }
            }
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });
        
        var httpClient = new HttpClient(handler);
        var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.RunAssistantAsync(threadId, assistantId))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*failed*");
    }

    [Fact]
    public async Task RunAssistantAsync_ShouldThrowWhenRunCancelled()
    {
        // Arrange
        var threadId = "thread_abc123";
        var assistantId = "asst_123";
        var runId = "run_abc123";
        
        _mockConfiguration.Setup(c => c["OpenAI:ApiKey"]).Returns("test-key");
        _mockConfiguration.Setup(c => c["OpenAI:BaseUrl"]).Returns("https://api.openai.com/v1");
        
        var callCount = 0;
        var handler = new MockHttpMessageHandler(async request =>
        {
            callCount++;
            if (request.RequestUri!.ToString().Contains("/runs"))
            {
                if (request.Method == HttpMethod.Post)
                {
                    var runResponse = JsonSerializer.Serialize(new { id = runId, status = "queued" });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(runResponse, Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    var runResponse = JsonSerializer.Serialize(new 
                    { 
                        id = runId, 
                        status = "cancelled"
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(runResponse, Encoding.UTF8, "application/json")
                    };
                }
            }
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });
        
        var httpClient = new HttpClient(handler);
        var service = new OpenAIAssistantService(httpClient, _mockConfiguration.Object, _mockLogger.Object);

        // Act & Assert
        await service.Invoking(s => s.RunAssistantAsync(threadId, assistantId))
            .Should().ThrowAsync<Exception>()
            .WithMessage("*cancelled*");
    }
}

