using FluentAssertions;
using Moq;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Services;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Tests.Unit.Application;

public class ConsultaServiceTests
{
    private const int UsuarioId = 1;
    private static readonly UsuarioAtual Tutor = new(UsuarioId, "Tutor");
    private static readonly DateTime Data = new(2026, 11, 4, 10, 0, 0);

    private readonly Mock<IConsultaRepository> _repositoryMock = new();
    private readonly Mock<IPetRepository> _petRepositoryMock = new();
    private readonly Mock<IRegistroAtividadeRepository> _atividadesMock = new();
    private readonly ConsultaService _service;

    public ConsultaServiceTests()
    {
        _service = new ConsultaService(_repositoryMock.Object, _petRepositoryMock.Object, _atividadesMock.Object);

        // Pet 1 pertence ao tutor dos testes
        _petRepositoryMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pet("Mimi", "Gato", "Siamês", 2, 4.2, UsuarioId));
    }

    private static CreateConsultaDto NovaConsultaDto(int petId = 1) => new()
    {
        PetId = petId,
        DataConsulta = Data,
        Veterinario = "Dra. Carla Souza",
        Motivo = "Rotina",
        Diagnostico = "Saudável"
    };

    [Fact]
    public async Task CadastrarAsync_ComPetExistente_DeveSalvarERegistrarAtividade()
    {
        // Arrange
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<Consulta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Consulta c, CancellationToken _) => c);

        // Act
        var resultado = await _service.CadastrarAsync(NovaConsultaDto(), Tutor);

        // Assert
        resultado.PetId.Should().Be(1);
        resultado.Veterinario.Should().Be("Dra. Carla Souza");
        _atividadesMock.Verify(a => a.RegistrarAsync(
            It.Is<RegistroAtividade>(r => r.Entidade == "Consulta" && r.Acao == RegistroAtividade.AcaoCriacao),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CadastrarAsync_ComPetInexistente_DeveLancarNotFoundENaoSalvar()
    {
        // Arrange
        _petRepositoryMock.Setup(r => r.ObterPorIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        // Act
        var cadastrar = () => _service.CadastrarAsync(NovaConsultaDto(petId: 42), Tutor);

        // Assert
        await cadastrar.Should().ThrowAsync<NotFoundException>().WithMessage("Pet*42*");
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Consulta>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CadastrarAsync_ParaPetDeOutroTutor_DeveLancarAcessoNegado()
    {
        // Arrange
        var outroTutor = new UsuarioAtual(99, "Tutor");

        // Act
        var cadastrar = () => _service.CadastrarAsync(NovaConsultaDto(), outroTutor);

        // Assert
        await cadastrar.Should().ThrowAsync<AcessoNegadoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Consulta>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CadastrarAsync_PorVeterinario_DevePermitirParaQualquerPet()
    {
        // Arrange
        var veterinario = new UsuarioAtual(50, "Veterinario");
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<Consulta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Consulta c, CancellationToken _) => c);

        // Act
        var resultado = await _service.CadastrarAsync(NovaConsultaDto(), veterinario);

        // Assert
        resultado.PetId.Should().Be(1);
    }

    [Fact]
    public async Task ListarAsync_ComPeriodoInvertido_DeveLancarDomainException()
    {
        // Arrange
        var filtro = new ConsultaFiltro { DataInicio = Data, DataFim = Data.AddDays(-1) };

        // Act
        var listar = () => _service.ListarAsync(filtro);

        // Assert
        await listar.Should().ThrowAsync<DomainException>();
        _repositoryMock.Verify(r => r.ListarAsync(It.IsAny<ConsultaFiltro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarAsync_ComFiltroValido_DeveConverterParaDto()
    {
        // Arrange
        var filtro = new ConsultaFiltro { PetId = 1, DataInicio = Data.AddDays(-1), DataFim = Data.AddDays(1) };
        var consultas = new List<Consulta> { new(1, Data, "Dra. Carla", "Rotina", null) };
        _repositoryMock.Setup(r => r.ListarAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Consulta>(consultas, 1, 1, 10));

        // Act
        var resultado = await _service.ListarAsync(filtro);

        // Assert
        resultado.Itens.Should().ContainSingle().Which.Veterinario.Should().Be("Dra. Carla");
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoConsultaNaoExiste_DeveLancarNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorIdAsync(77, It.IsAny<CancellationToken>())).ReturnsAsync((Consulta?)null);

        // Act
        var obter = () => _service.ObterPorIdAsync(77);

        // Assert
        await obter.Should().ThrowAsync<NotFoundException>().WithMessage("Consulta*77*");
    }

    [Fact]
    public async Task RemoverAsync_QuandoConsultaExiste_DeveRemoverERegistrarAtividade()
    {
        // Arrange
        var consulta = new Consulta(1, Data, "Dra. Carla", "Rotina", null);
        _repositoryMock.Setup(r => r.ObterPorIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(consulta);

        // Act
        await _service.RemoverAsync(4, Tutor);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(consulta, It.IsAny<CancellationToken>()), Times.Once);
        _atividadesMock.Verify(a => a.RegistrarAsync(
            It.Is<RegistroAtividade>(r => r.Acao == RegistroAtividade.AcaoRemocao), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_QuandoConsultaExiste_DeveAlterarDados()
    {
        // Arrange
        var consulta = new Consulta(1, Data, "Dra. Carla", "Rotina", null);
        _repositoryMock.Setup(r => r.ObterPorIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(consulta);
        var dto = new UpdateConsultaDto { DataConsulta = Data.AddDays(1), Veterinario = "Dr. Marcos", Motivo = "Retorno", Diagnostico = "Ok" };

        // Act
        var resultado = await _service.AtualizarAsync(3, dto, Tutor);

        // Assert
        resultado.Veterinario.Should().Be("Dr. Marcos");
        resultado.DataConsulta.Should().Be(Data.AddDays(1));
        _repositoryMock.Verify(r => r.AtualizarAsync(consulta, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoverAsync_QuandoConsultaNaoExiste_DeveLancarNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Consulta?)null);

        // Act
        var remover = () => _service.RemoverAsync(9, Tutor);

        // Assert
        await remover.Should().ThrowAsync<NotFoundException>();
        _atividadesMock.Verify(a => a.RegistrarAsync(It.IsAny<RegistroAtividade>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
