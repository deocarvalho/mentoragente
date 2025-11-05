using FluentAssertions;
using Mentoragente.Domain.Entities;
using Mentoragente.Domain.Enums;
using Xunit;

namespace Mentoragente.Tests.Domain.Entities;

public class MentoriaTests
{
    [Fact]
    public void Mentoria_ShouldCreateWithDefaultValues()
    {
        // Arrange & Act
        var mentoria = new Mentoria();

        // Assert
        mentoria.Id.Should().NotBeEmpty();
        mentoria.Nome.Should().BeEmpty();
        mentoria.MentorId.Should().BeEmpty();
        mentoria.AssistantId.Should().BeEmpty();
        mentoria.DuracaoDias.Should().Be(0);
        mentoria.Descricao.Should().BeNull();
        mentoria.Status.Should().Be(MentoriaStatus.Active);
        mentoria.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        mentoria.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Mentoria_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mentorId = Guid.NewGuid();
        var nome = "Nina - Descoberta de Oferta";
        var assistantId = "asst_ABC123";
        var duracaoDias = 30;
        var descricao = "Programa de 30 dias";
        var status = MentoriaStatus.Active;

        // Act
        var mentoria = new Mentoria
        {
            Id = id,
            MentorId = mentorId,
            Nome = nome,
            AssistantId = assistantId,
            DuracaoDias = duracaoDias,
            Descricao = descricao,
            Status = status
        };

        // Assert
        mentoria.Id.Should().Be(id);
        mentoria.MentorId.Should().Be(mentorId);
        mentoria.Nome.Should().Be(nome);
        mentoria.AssistantId.Should().Be(assistantId);
        mentoria.DuracaoDias.Should().Be(duracaoDias);
        mentoria.Descricao.Should().Be(descricao);
        mentoria.Status.Should().Be(status);
    }
}

