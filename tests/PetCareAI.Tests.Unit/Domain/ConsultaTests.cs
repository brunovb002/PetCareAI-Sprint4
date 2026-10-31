using FluentAssertions;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Tests.Unit.Domain;

public class ConsultaTests
{
    private static readonly DateTime Data = new(2026, 11, 4, 10, 0, 0);

    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedades()
    {
        // Arrange + Act
        var consulta = new Consulta(1, Data, "Dra. Carla", "Rotina", "Saudável");

        // Assert
        consulta.PetId.Should().Be(1);
        consulta.DataConsulta.Should().Be(Data);
        consulta.Veterinario.Should().Be("Dra. Carla");
        consulta.Diagnostico.Should().Be("Saudável");
    }

    [Fact]
    public void Criar_SemDiagnostico_DeveAceitarComoVazio()
    {
        // Arrange + Act
        var consulta = new Consulta(1, Data, "Dra. Carla", "Rotina", null);

        // Assert
        consulta.Diagnostico.Should().BeEmpty();
    }

    [Fact]
    public void Criar_SemPet_DeveLancarDomainException()
    {
        // Arrange
        var criar = () => new Consulta(0, Data, "Dra. Carla", "Rotina", null);

        // Act + Assert
        criar.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_SemData_DeveLancarDomainException()
    {
        // Arrange
        var criar = () => new Consulta(1, default, "Dra. Carla", "Rotina", null);

        // Act + Assert
        criar.Should().Throw<DomainException>().WithMessage("Data da consulta é obrigatória.");
    }

    [Fact]
    public void Atualizar_ComMotivoVazio_DeveLancarDomainException()
    {
        // Arrange
        var consulta = new Consulta(1, Data, "Dra. Carla", "Rotina", null);

        // Act
        var atualizar = () => consulta.Atualizar(Data, "Dra. Carla", "", null);

        // Assert
        atualizar.Should().Throw<DomainException>().WithMessage("Motivo é obrigatório.");
    }
}
