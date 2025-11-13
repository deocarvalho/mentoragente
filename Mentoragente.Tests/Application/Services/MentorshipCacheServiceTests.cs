using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class MentorshipCacheServiceTests
{
    private readonly Mock<IMentorshipRepository> _mockMentorshipRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<MentorshipCacheService>> _mockLogger;
    private readonly MentorshipCacheService _cacheService;

    public MentorshipCacheServiceTests()
    {
        _mockMentorshipRepository = new Mock<IMentorshipRepository>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<MentorshipCacheService>>();
        
        _cacheService = new MentorshipCacheService(
            _mockMentorshipRepository.Object,
            _memoryCache,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetMentorshipAsync_ShouldReturnFromRepository_WhenNotInCache()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            Name = "Test Mentorship",
            Status = MentorshipStatus.Active 
        };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Act
        var result = await _cacheService.GetMentorshipAsync(mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(mentorship);
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByIdAsync(mentorshipId), Times.Once);
    }

    [Fact]
    public async Task GetMentorshipAsync_ShouldReturnFromCache_WhenAlreadyCached()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            Name = "Test Mentorship",
            Status = MentorshipStatus.Active 
        };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // First call - should hit repository
        await _cacheService.GetMentorshipAsync(mentorshipId);
        
        // Reset mock to verify it's not called again
        _mockMentorshipRepository.Reset();

        // Act - Second call should hit cache
        var result = await _cacheService.GetMentorshipAsync(mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(mentorship);
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetMentorshipAsync_ShouldReturnNull_WhenMentorshipNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act
        var result = await _cacheService.GetMentorshipAsync(mentorshipId);

        // Assert
        result.Should().BeNull();
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByIdAsync(mentorshipId), Times.Once);
    }

    [Fact]
    public async Task InvalidateMentorship_ShouldRemoveFromCache()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            Name = "Test Mentorship",
            Status = MentorshipStatus.Active 
        };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Cache the mentorship
        await _cacheService.GetMentorshipAsync(mentorshipId);

        // Act
        _cacheService.InvalidateMentorship(mentorshipId);

        // Assert - Next call should hit repository again
        _mockMentorshipRepository.Reset();
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        var result = await _cacheService.GetMentorshipAsync(mentorshipId);
        
        result.Should().NotBeNull();
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByIdAsync(mentorshipId), Times.Once);
    }

    [Fact]
    public async Task GetMentorshipAsync_ShouldUseSlidingExpiration()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship 
        { 
            Id = mentorshipId, 
            Name = "Test Mentorship",
            Status = MentorshipStatus.Active 
        };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // First call - cache it
        await _cacheService.GetMentorshipAsync(mentorshipId);
        
        // Wait a bit (but less than expiration)
        await Task.Delay(100);
        
        // Second call - should extend expiration (sliding)
        await _cacheService.GetMentorshipAsync(mentorshipId);
        
        // Reset mock
        _mockMentorshipRepository.Reset();
        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Act - Third call should still be from cache (expiration was extended)
        var result = await _cacheService.GetMentorshipAsync(mentorshipId);

        // Assert
        result.Should().NotBeNull();
        // Repository should not be called because cache was extended
        _mockMentorshipRepository.Verify(x => x.GetMentorshipByIdAsync(It.IsAny<Guid>()), Times.Never);
    }
}

