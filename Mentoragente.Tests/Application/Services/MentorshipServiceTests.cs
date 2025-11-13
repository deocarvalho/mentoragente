using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class MentorshipServiceTests
{
    private readonly Mock<IMentorshipRepository> _mockMentorshipRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<MentorshipService>> _mockLogger;
    private readonly MentorshipService _mentorshipService;

    public MentorshipServiceTests()
    {
        _mockMentorshipRepository = new Mock<IMentorshipRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<MentorshipService>>();
        _mentorshipService = new MentorshipService(_mockMentorshipRepository.Object, _mockUserRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateMentorshipAsync_ShouldThrowWhenMentorNotFound()
    {
        // Arrange
        var mentorId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _mentorshipService.Invoking(s => s.CreateMentorshipAsync(mentorId, "Test Mentorship", "asst_123", 30, null, null, "instance_code", null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mentor with ID {mentorId} not found*");
    }

    [Fact]
    public async Task CreateMentorshipAsync_ShouldCreateMentorshipSuccessfully()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentor = new User { Id = mentorId, Name = "Mentor" };
        var name = "Test Mentorship";
        var assistantId = "asst_123";
        var durationDays = 30;
        var instanceCode = "test_instance";

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync(mentor);

        _mockMentorshipRepository.Setup(x => x.CreateMentorshipAsync(It.IsAny<Mentorship>()))
            .ReturnsAsync((Mentorship m) => m);

        // Act
        var result = await _mentorshipService.CreateMentorshipAsync(mentorId, name, assistantId, durationDays, null, WhatsAppProvider.ZApi, instanceCode, null);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.AssistantId.Should().Be(assistantId);
        result.DurationDays.Should().Be(durationDays);
        result.InstanceCode.Should().Be(instanceCode);
        result.WhatsAppProvider.Should().Be(WhatsAppProvider.ZApi);
        result.Status.Should().Be(MentorshipStatus.Active);
        _mockMentorshipRepository.Verify(x => x.CreateMentorshipAsync(It.IsAny<Mentorship>()), Times.Once);
    }

    [Fact]
    public async Task GetMentorshipsByMentorIdAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var page = 1;
        var pageSize = 10;
        var mentorships = new List<Mentorship>
        {
            new Mentorship { Id = Guid.NewGuid(), Name = "Mentorship 1", MentorId = mentorId },
            new Mentorship { Id = Guid.NewGuid(), Name = "Mentorship 2", MentorId = mentorId }
        };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipsByMentorIdAsync(mentorId, 0, pageSize))
            .ReturnsAsync(mentorships);
        _mockMentorshipRepository.Setup(x => x.GetMentorshipsCountByMentorIdAsync(mentorId))
            .ReturnsAsync(2);

        // Act
        var result = await _mentorshipService.GetMentorshipsByMentorIdAsync(mentorId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task DeleteMentorshipAsync_ShouldSoftDelete()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship { Id = mentorshipId, Status = MentorshipStatus.Active };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockMentorshipRepository.Setup(x => x.UpdateMentorshipAsync(It.IsAny<Mentorship>()))
            .ReturnsAsync((Mentorship m) => m);

        // Act
        var result = await _mentorshipService.DeleteMentorshipAsync(mentorshipId);

        // Assert
        result.Should().BeTrue();
        _mockMentorshipRepository.Verify(x => x.UpdateMentorshipAsync(
            It.Is<Mentorship>(m => m.Status == MentorshipStatus.Archived)), Times.Once);
    }

    [Fact]
    public async Task DeleteMentorshipAsync_ShouldReturnFalseWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act
        var result = await _mentorshipService.DeleteMentorshipAsync(mentorshipId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetMentorshipByIdAsync_ShouldReturnMentorship()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship { Id = mentorshipId, Name = "Test Mentorship" };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        // Act
        var result = await _mentorshipService.GetMentorshipByIdAsync(mentorshipId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(mentorshipId);
    }

    [Fact]
    public async Task GetMentorshipsByMentorIdAsync_ShouldReturnList()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentorships = new List<Mentorship>
        {
            new Mentorship { Id = Guid.NewGuid(), MentorId = mentorId },
            new Mentorship { Id = Guid.NewGuid(), MentorId = mentorId }
        };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipsByMentorIdAsync(mentorId))
            .ReturnsAsync(mentorships);

        // Act
        var result = await _mentorshipService.GetMentorshipsByMentorIdAsync(mentorId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveMentorshipsAsync_ShouldReturnActiveOnly()
    {
        // Arrange
        var allMentorships = new List<Mentorship>
        {
            new Mentorship { Id = Guid.NewGuid(), Status = MentorshipStatus.Active },
            new Mentorship { Id = Guid.NewGuid(), Status = MentorshipStatus.Inactive },
            new Mentorship { Id = Guid.NewGuid(), Status = MentorshipStatus.Active }
        };

        _mockMentorshipRepository.Setup(x => x.GetAllMentorshipsAsync())
            .ReturnsAsync(allMentorships);

        // Act
        var result = await _mentorshipService.GetActiveMentorshipsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(m => m.Status == MentorshipStatus.Active).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveMentorshipsAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;
        var mentorships = new List<Mentorship>
        {
            new Mentorship { Id = Guid.NewGuid(), Status = MentorshipStatus.Active }
        };

        _mockMentorshipRepository.Setup(x => x.GetActiveMentorshipsAsync(0, pageSize))
            .ReturnsAsync(mentorships);
        _mockMentorshipRepository.Setup(x => x.GetActiveMentorshipsCountAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _mentorshipService.GetActiveMentorshipsAsync(page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateMentorshipAsync_ShouldThrowWhenNameIsInvalid(string? name)
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentor = new User { Id = mentorId };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync(mentor);

        // Act & Assert
        await _mentorshipService.Invoking(s => s.CreateMentorshipAsync(mentorId, name, "asst_123", 30, null, null, "instance", null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Name is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateMentorshipAsync_ShouldThrowWhenAssistantIdIsInvalid(string? assistantId)
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentor = new User { Id = mentorId };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync(mentor);

        // Act & Assert
        await _mentorshipService.Invoking(s => s.CreateMentorshipAsync(mentorId, "Test", assistantId, 30, null, null, "instance", null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Assistant ID is required*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateMentorshipAsync_ShouldThrowWhenDurationDaysIsInvalid(int durationDays)
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentor = new User { Id = mentorId };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync(mentor);

        // Act & Assert
        await _mentorshipService.Invoking(s => s.CreateMentorshipAsync(mentorId, "Test", "asst_123", durationDays, null, null, "instance", null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Duration in days must be greater than 0*");
    }

    [Fact]
    public async Task UpdateMentorshipAsync_ShouldUpdateName()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship { Id = mentorshipId, Name = "Old Name" };
        var newName = "New Name";

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockMentorshipRepository.Setup(x => x.UpdateMentorshipAsync(It.IsAny<Mentorship>()))
            .ReturnsAsync((Mentorship m) => m);

        // Act
        var result = await _mentorshipService.UpdateMentorshipAsync(mentorshipId, name: newName);

        // Assert
        result.Name.Should().Be(newName);
    }

    [Fact]
    public async Task UpdateMentorshipAsync_ShouldUpdateStatus()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();
        var mentorship = new Mentorship { Id = mentorshipId, Status = MentorshipStatus.Active };

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync(mentorship);

        _mockMentorshipRepository.Setup(x => x.UpdateMentorshipAsync(It.IsAny<Mentorship>()))
            .ReturnsAsync((Mentorship m) => m);

        // Act
        var result = await _mentorshipService.UpdateMentorshipAsync(mentorshipId, status: MentorshipStatus.Inactive);

        // Assert
        result.Status.Should().Be(MentorshipStatus.Inactive);
    }

    [Fact]
    public async Task UpdateMentorshipAsync_ShouldThrowWhenNotFound()
    {
        // Arrange
        var mentorshipId = Guid.NewGuid();

        _mockMentorshipRepository.Setup(x => x.GetMentorshipByIdAsync(mentorshipId))
            .ReturnsAsync((Mentorship?)null);

        // Act & Assert
        await _mentorshipService.Invoking(s => s.UpdateMentorshipAsync(mentorshipId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mentorship with ID {mentorshipId} not found*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateMentorshipAsync_ShouldThrowWhenInstanceCodeIsInvalid(string? instanceCode)
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentor = new User { Id = mentorId };

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync(mentor);

        // Act & Assert
        await _mentorshipService.Invoking(s => s.CreateMentorshipAsync(mentorId, "Test", "asst_123", 30, null, null, instanceCode, null))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Instance code is required*");
    }
}

