using FluentAssertions;
using PetCareAI.Application.Mappings;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Tests.Unit.Application;

public class ClyvoAiTraducaoTests
{
    [Theory]
    [InlineData("Cachorro", "Dog")]
    [InlineData("cão", "Dog")]
    [InlineData("GATO", "cat")]
    public void Especie_DeveTraduzirIgnorandoAcentosEMaiusculas(string especie, string esperado)
    {
        // Arrange + Act
        var traduzida = ClyvoAiTraducao.Especie(especie);

        // Assert
        traduzida.Should().Be(esperado);
    }

    [Fact]
    public void Especie_ForaDoModelo_DeveLancarDomainException()
    {
        // Arrange
        var traduzir = () => ClyvoAiTraducao.Especie("Hamster");

        // Act + Assert
        traduzir.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("SRD", "Mixed Breed")]
    [InlineData("Pastor Alemão", "German Shepherd")]
    [InlineData("Golden Retriever", "Golden Retriever")]
    public void Raca_DeveTraduzirOuManterOriginal(string raca, string esperado)
    {
        // Arrange + Act
        var traduzida = ClyvoAiTraducao.Raca(raca);

        // Assert
        traduzida.Should().Be(esperado);
    }

    [Theory]
    [InlineData("Vômito", "Vomiting")]
    [InlineData("febre alta há 2 dias", "Fever")]
    [InlineData("está com dificuldade para respirar", "Difficulty in breathing")]
    [InlineData("perda de apetite", "Loss of appetite")]
    public void Sintoma_DeveEncontrarTermoConhecidoNoTexto(string sintoma, string esperado)
    {
        // Arrange + Act
        var traduzido = ClyvoAiTraducao.Sintoma(sintoma);

        // Assert
        traduzido.Should().Be(esperado);
    }

    [Theory]
    [InlineData("dormindo muito")]
    [InlineData("olhos lacrimejando")]
    public void Sintoma_Desconhecido_DeveRetornarNulo(string sintoma)
    {
        // Arrange + Act
        var traduzido = ClyvoAiTraducao.Sintoma(sintoma);

        // Assert
        traduzido.Should().BeNull();
    }
}
