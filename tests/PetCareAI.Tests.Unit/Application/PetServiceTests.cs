using FluentAssertions;
using Moq;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Services;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Tests.Unit.Application;

public class PetServiceTests
{
    private const int UsuarioId = 1;

    private readonly Mock<IPetRepository> _repositoryMock = new();
    private readonly Mock<IRegistroAtividadeRepository> _atividadesMock = new();
    private readonly PetService _service;

    public PetServiceTests()
    {
        _service = new PetService(_repositoryMock.Object, _atividadesMock.Object);
    }

    private static CreatePetDto NovoPetDto() => new()
    {
        Nome = "Thor",
        Especie = "Cachorro",
        Raca = "Golden Retriever",
        Idade = 3,
        Peso = 30.5
    };

    [Fact]
    public async Task CadastrarAsync_ComDadosValidos_DeveSalvarERegistrarAtividade()
    {
        // Arrange
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pet p, CancellationToken _) => p);

        // Act
        var resultado = await _service.CadastrarAsync(NovoPetDto(), UsuarioId);

        // Assert
        resultado.Nome.Should().Be("Thor");
        resultado.UsuarioId.Should().Be(UsuarioId);
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Once);
        _atividadesMock.Verify(a => a.RegistrarAsync(
            It.Is<RegistroAtividade>(r => r.Entidade == "Pet" && r.Acao == RegistroAtividade.AcaoCriacao && r.UsuarioId == UsuarioId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CadastrarAsync_ComDadosInvalidos_NaoDeveSalvar()
    {
        // Arrange
        var dto = NovoPetDto();
        dto.Peso = 0;

        // Act
        var cadastrar = () => _service.CadastrarAsync(dto, UsuarioId);

        // Assert
        await cadastrar.Should().ThrowAsync<DomainException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoPetNaoExiste_DeveLancarNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        // Act
        var obter = () => _service.ObterPorIdAsync(999);

        // Assert
        await obter.Should().ThrowAsync<NotFoundException>().WithMessage("*999*");
    }

    [Fact]
    public async Task AtualizarAsync_QuandoPetExiste_DeveAlterarSalvarERegistrarAtividade()
    {
        // Arrange
        var pet = new Pet("Thor", "Cachorro", "Golden Retriever", 3, 30.5, UsuarioId);
        _repositoryMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        var dto = new UpdatePetDto { Nome = "Thor", Especie = "Cachorro", Raca = "Golden Retriever", Idade = 4, Peso = 32 };

        // Act
        var resultado = await _service.AtualizarAsync(1, dto, UsuarioId);

        // Assert
        resultado.Idade.Should().Be(4);
        resultado.Peso.Should().Be(32);
        _repositoryMock.Verify(r => r.AtualizarAsync(pet, It.IsAny<CancellationToken>()), Times.Once);
        _atividadesMock.Verify(a => a.RegistrarAsync(
            It.Is<RegistroAtividade>(r => r.Acao == RegistroAtividade.AcaoAtualizacao), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoPetNaoExiste_DeveLancarNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        // Act
        var atualizar = () => _service.AtualizarAsync(5, new UpdatePetDto(), UsuarioId);

        // Assert
        await atualizar.Should().ThrowAsync<NotFoundException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoverAsync_QuandoPetExiste_DeveRemoverERegistrarAtividade()
    {
        // Arrange
        var pet = new Pet("Mimi", "Gato", "Siamês", 2, 4.2, UsuarioId);
        _repositoryMock.Setup(r => r.ObterPorIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(pet);

        // Act
        await _service.RemoverAsync(2, UsuarioId);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(pet, It.IsAny<CancellationToken>()), Times.Once);
        _atividadesMock.Verify(a => a.RegistrarAsync(
            It.Is<RegistroAtividade>(r => r.Acao == RegistroAtividade.AcaoRemocao), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_DeveRepassarFiltroEConverterParaDto()
    {
        // Arrange
        var filtro = new PetFiltro { Especie = "Gato", Pagina = 1, TamanhoPagina = 5 };
        var pets = new List<Pet> { new("Mimi", "Gato", "Siamês", 2, 4.2, UsuarioId) };
        _repositoryMock.Setup(r => r.ListarAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Pet>(pets, 1, 1, 5));

        // Act
        var resultado = await _service.ListarAsync(filtro);

        // Assert
        resultado.TotalItens.Should().Be(1);
        resultado.Itens.Should().ContainSingle().Which.Nome.Should().Be("Mimi");
    }
}
