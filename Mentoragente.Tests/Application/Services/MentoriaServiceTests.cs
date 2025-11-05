using FluentAssertions;
using Xunit;
using Moq;
using Mentoragente.Application.Services;
using Mentoragente.Domain.Interfaces;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Mentoragente.Tests.Application.Services;

public class MentoriaServiceTests
{
    private readonly Mock<IMentoriaRepository> _mockMentoriaRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<MentoriaService>> _mockLogger;
    private readonly MentoriaService _mentoriaService;

    public MentoriaServiceTests()
    {
        _mockMentoriaRepository = new Mock<IMentoriaRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<MentoriaService>>();
        _mentoriaService = new MentoriaService(_mockMentoriaRepository.Object, _mockUserRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateMentoriaAsync_ShouldThrowWhenMentorNotFound()
    {
        // Arrange
        var mentorId = Guid.NewGuid();

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await _mentoriaService.Invoking(s => s.CreateMentoriaAsync(mentorId, "Test Mentoria", "asst_123", 30))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Mentor with ID {mentorId} not found*");
    }

    [Fact]
    public async Task CreateMentoriaAsync_ShouldCreateMentoriaSuccessfully()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var mentor = new User { Id = mentorId, Name = "Mentor" };
        var nome = "Test Mentoria";
        var assistantId = "asst_123";
        var duracaoDias = 30;

        _mockUserRepository.Setup(x => x.GetUserByIdAsync(mentorId))
            .ReturnsAsync(mentor);

        _mockMentoriaRepository.Setup(x => x.CreateMentoriaAsync(It.IsAny<Mentoria>()))
            .ReturnsAsync((Mentoria m) => m);

        // Act
        var result = await _mentoriaService.CreateMentoriaAsync(mentorId, nome, assistantId, duracaoDias);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(nome);
        result.AssistantId.Should().Be(assistantId);
        result.DuracaoDias.Should().Be(duracaoDias);
        result.Status.Should().Be(MentoriaStatus.Active);
        _mockMentoriaRepository.Verify(x => x.CreateMentoriaAsync(It.IsAny<Mentoria>()), Times.Once);
    }

    [Fact]
    public async Task GetMentoriasByMentorIdAsync_ShouldReturnPagedResult()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var page = 1;
        var pageSize = 10;
        var mentorias = new List<Mentoria>
        {
            new Mentoria { Id = Guid.NewGuid(), Nome = "Mentoria 1", MentorId = mentorId },
            new Mentoria { Id = Guid.NewGuid(), Nome = "Mentoria 2", MentorId = mentorId }
        };

        _mockMentoriaRepository.Setup(x => x.GetMentoriasByMentorIdAsync(mentorId, 0, pageSize))
            .ReturnsAsync(mentorias);
        _mockMentoriaRepository.Setup(x => x.GetMentoriasCountByMentorIdAsync(mentorId))
            .ReturnsAsync(2);

        // Act
        var result = await _mentoriaService.GetMentoriasByMentorIdAsync(mentorId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task DeleteMentoriaAsync_ShouldSoftDelete()
    {
        // Arrange
        var mentoriaId = Guid.NewGuid();
        var mentoria = new Mentoria { Id = mentoriaId, Status = MentoriaStatus.Active };

        _mockMentoriaRepository.Setup(x => x.GetMentoriaByIdAsync(mentoriaId))
            .ReturnsAsync(mentoria);

        _mockMentoriaRepository.Setup(x => x.UpdateMentoriaAsync(It.IsAny<Mentoria>()))
            .ReturnsAsync((Mentoria m) => m);

        // Act
        var result = await _mentoriaService.DeleteMentoriaAsync(mentoriaId);

        // Assert
        result.Should().BeTrue();
        _mockMentoriaRepository.Verify(x => x.UpdateMentoriaAsync(
            It.Is<Mentoria>(m => m.Status == MentoriaStatus.Archived)), Times.Once);
    }
}

