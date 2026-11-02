using FluentAssertions;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Tests.Unit.Domain;

public class PetTests
{
    [Fact]
    public void Criar_ComDadosValidos_DevePreencherPropriedades()
    {
        // Arrange
        const string nome = "  Thor  ";

        // Act
        var pet = new Pet(nome, "Cachorro", "Golden Retriever", 3, 28.5, usuarioId: 1);

        // Assert
        pet.Nome.Should().Be("Thor");
        pet.Especie.Should().Be("Cachorro");
        pet.Idade.Should().Be(3);
        pet.Peso.Should().Be(28.5);
        pet.UsuarioId.Should().Be(1);
    }

    [Theory]
    [InlineData("", "Cachorro", "SRD", 3, 10, "Nome é obrigatório.")]
    [InlineData("Thor", " ", "SRD", 3, 10, "Espécie é obrigatório.")]
    [InlineData("Thor", "Cachorro", "SRD", -1, 10, "Idade deve estar entre 0 e 40 anos.")]
    [InlineData("Thor", "Cachorro", "SRD", 3, 0, "Peso deve ser maior que 0 e no máximo 200 kg.")]
    public void Criar_ComDadosInvalidos_DeveLancarDomainException(
        string nome, string especie, string raca, int idade, double peso, string mensagemEsperada)
    {
        // Arrange
        var criar = () => new Pet(nome, especie, raca, idade, peso, usuarioId: 1);

        // Act + Assert
        criar.Should().Throw<DomainException>().WithMessage(mensagemEsperada);
    }

    [Fact]
    public void Criar_SemTutor_DeveLancarDomainException()
    {
        // Arrange
        var criar = () => new Pet("Thor", "Cachorro", "SRD", 3, 10, usuarioId: 0);

        // Act + Assert
        criar.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_ComNomeMuitoLongo_DeveLancarDomainException()
    {
        // Arrange
        var nomeLongo = new string('a', Pet.NomeMax + 1);

        // Act
        var criar = () => new Pet(nomeLongo, "Gato", "Siamês", 2, 4, usuarioId: 1);

        // Assert
        criar.Should().Throw<DomainException>().WithMessage($"*{Pet.NomeMax}*");
    }

    [Fact]
    public void Atualizar_ComDadosValidos_DeveAlterarDadosEManterTutor()
    {
        // Arrange
        var pet = new Pet("Mimi", "Gato", "Siamês", 2, 4.2, usuarioId: 7);

        // Act
        pet.Atualizar("Mimi", "Gato", "Siamês", 3, 4.8);

        // Assert
        pet.Idade.Should().Be(3);
        pet.Peso.Should().Be(4.8);
        pet.UsuarioId.Should().Be(7);
    }

    [Fact]
    public void Atualizar_ComDadosInvalidos_NaoDeveAlterarEstado()
    {
        // Arrange
        var pet = new Pet("Mimi", "Gato", "Siamês", 2, 4.2, usuarioId: 1);

        // Act
        var atualizar = () => pet.Atualizar("Mimi", "Gato", "Siamês", 2, -5);

        // Assert
        atualizar.Should().Throw<DomainException>();
        pet.Peso.Should().Be(4.2);
    }
}
