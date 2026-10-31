using FluentAssertions;
using PetCareAI.Domain.Common;

namespace PetCareAI.Tests.Unit.Domain;

public class PaginacaoTests
{
    [Fact]
    public void Filtro_ComValoresForaDoLimite_DeveAjustarPaginaETamanho()
    {
        // Arrange
        var filtro = new PetFiltro();

        // Act
        filtro.Pagina = 0;
        filtro.TamanhoPagina = 1000;

        // Assert
        filtro.Pagina.Should().Be(1);
        filtro.TamanhoPagina.Should().Be(FiltroPaginado.TamanhoPaginaMaximo);
    }

    [Theory]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("asc", false)]
    [InlineData(null, false)]
    public void Filtro_Direcao_DeveIndicarSeEDescendente(string? direcao, bool esperado)
    {
        // Arrange
        var filtro = new PetFiltro { Direcao = direcao };

        // Act
        var descendente = filtro.Descendente;

        // Assert
        descendente.Should().Be(esperado);
    }

    [Fact]
    public void Filtro_Saltar_DeveCalcularDeslocamentoDaPagina()
    {
        // Arrange
        var filtro = new PetFiltro { Pagina = 3, TamanhoPagina = 10 };

        // Act
        var saltar = filtro.Saltar;

        // Assert
        saltar.Should().Be(20);
    }

    [Fact]
    public void PagedResult_DeveCalcularTotalDePaginasENavegacao()
    {
        // Arrange
        var pagina = new PagedResult<int>([4, 5, 6], totalItens: 7, pagina: 2, tamanhoPagina: 3);

        // Act + Assert
        pagina.TotalPaginas.Should().Be(3);
        pagina.TemAnterior.Should().BeTrue();
        pagina.TemProxima.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_Vazio_NaoDeveTerPaginas()
    {
        // Arrange
        var pagina = new PagedResult<int>([], totalItens: 0, pagina: 1, tamanhoPagina: 10);

        // Act + Assert
        pagina.TotalPaginas.Should().Be(0);
        pagina.TemProxima.Should().BeFalse();
        pagina.TemAnterior.Should().BeFalse();
    }

    [Fact]
    public void PagedResult_Map_DeveConverterItensMantendoMetadados()
    {
        // Arrange
        var pagina = new PagedResult<int>([1, 2], totalItens: 5, pagina: 1, tamanhoPagina: 2);

        // Act
        var convertida = pagina.Map(i => $"item-{i}");

        // Assert
        convertida.Itens.Should().Equal("item-1", "item-2");
        convertida.TotalItens.Should().Be(5);
        convertida.TotalPaginas.Should().Be(3);
    }
}
