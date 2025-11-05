using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mentoragente.API.Controllers;
using Mentoragente.Application.Services;
using Mentoragente.Domain.DTOs;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Mentoragente.Application.Models;
using FluentValidation;

namespace Mentoragente.Tests.API.Controllers;

public class MentoriasControllerTests
{
    private readonly Mock<IMentoriaService> _mockMentoriaService;
    private readonly Mock<ILogger<MentoriasController>> _mockLogger;
    private readonly Mock<IValidator<CreateMentoriaRequestDto>> _mockCreateValidator;
    private readonly Mock<IValidator<UpdateMentoriaRequestDto>> _mockUpdateValidator;
    private readonly MentoriasController _controller;

    public MentoriasControllerTests()
    {
        _mockMentoriaService = new Mock<IMentoriaService>();
        _mockLogger = new Mock<ILogger<MentoriasController>>();
        _mockCreateValidator = new Mock<IValidator<CreateMentoriaRequestDto>>();
        _mockUpdateValidator = new Mock<IValidator<UpdateMentoriaRequestDto>>();

        _controller = new MentoriasController(
            _mockMentoriaService.Object,
            _mockLogger.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object);
    }

    [Fact]
    public async Task GetMentoriasByMentorId_ShouldReturnPagedResult()
    {
        // Arrange
        var mentorId = Guid.NewGuid();
        var pagination = new PaginationRequestDto { Page = 1, PageSize = 10 };
        var mentorias = new List<Mentoria>
        {
            new Mentoria { Id = Guid.NewGuid(), Nome = "Mentoria 1", MentorId = mentorId }
        };
        var pagedResult = PagedResult<Mentoria>.Create(mentorias, 1, 1, 10);

        _mockMentoriaService.Setup(x => x.GetMentoriasByMentorIdAsync(mentorId, 1, 10))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetMentoriasByMentorId(mentorId, pagination);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateMentoria_ShouldReturnCreatedWhenSuccessful()
    {
        // Arrange
        var request = new CreateMentoriaRequestDto
        {
            MentorId = Guid.NewGuid(),
            Nome = "Test Mentoria",
            AssistantId = "asst_123",
            DuracaoDias = 30
        };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var mentoria = new Mentoria
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            MentorId = request.MentorId,
            AssistantId = request.AssistantId,
            DuracaoDias = request.DuracaoDias
        };

        _mockCreateValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _mockMentoriaService.Setup(x => x.CreateMentoriaAsync(
            request.MentorId, request.Nome, request.AssistantId, request.DuracaoDias, request.Descricao))
            .ReturnsAsync(mentoria);

        // Act
        var result = await _controller.CreateMentoria(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task DeleteMentoria_ShouldReturnNoContentWhenSuccessful()
    {
        // Arrange
        var mentoriaId = Guid.NewGuid();

        _mockMentoriaService.Setup(x => x.DeleteMentoriaAsync(mentoriaId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteMentoria(mentoriaId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}

