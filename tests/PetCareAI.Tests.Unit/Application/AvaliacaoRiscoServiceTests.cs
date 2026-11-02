using FluentAssertions;
using Moq;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Exceptions;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Services;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Tests.Unit.Application;

public class AvaliacaoRiscoServiceTests
{
    private const int UsuarioId = 1;
    private static readonly UsuarioAtual Tutor = new(UsuarioId, "Tutor");

    private readonly Mock<IPetRepository> _petRepositoryMock = new();
    private readonly Mock<IAvaliacaoRiscoRepository> _avaliacoesMock = new();
    private readonly Mock<IClyvoAiClient> _clyvoAiMock = new();
    private readonly Mock<IRegistroAtividadeRepository> _atividadesMock = new();
    private readonly AvaliacaoRiscoService _service;

    public AvaliacaoRiscoServiceTests()
    {
        _service = new AvaliacaoRiscoService(_petRepositoryMock.Object, _avaliacoesMock.Object, _clyvoAiMock.Object, _atividadesMock.Object);
        _avaliacoesMock.Setup(a => a.AdicionarAsync(It.IsAny<AvaliacaoRisco>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AvaliacaoRisco a, CancellationToken _) => a);
    }

    private void PetCadastrado(string especie = "Gato", string raca = "Siamês", int idade = 2) =>
        _petRepositoryMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pet("Mimi", especie, raca, idade, 4.2, UsuarioId));

    private void IaRetorna(string prioridade, double confianca) =>
        _clyvoAiMock.Setup(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrevisaoClyvoAi(prioridade, new Dictionary<string, double> { [prioridade] = confianca }));

    private static AvaliacaoRiscoRequestDto Requisicao(HistoricoMedico historico, params string[] sintomas) =>
        new() { HistoricoMedico = historico, Sintomas = sintomas.ToList() };

    [Fact]
    public async Task AvaliarAsync_DeveTraduzirDadosDoPetESintomasParaOFormatoDoModelo()
    {
        // Arrange
        PetCadastrado();
        EntradaClyvoAi? enviada = null;
        _clyvoAiMock.Setup(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()))
            .Callback((EntradaClyvoAi e, CancellationToken _) => enviada = e)
            .ReturnsAsync(new PrevisaoClyvoAi("low", new Dictionary<string, double> { ["low"] = 0.997 }));

        // Act
        await _service.AvaliarAsync(1, Requisicao(HistoricoMedico.SemCondicoesPrevias, "Espirros", "tosse leve"), Tutor);

        // Assert
        enviada!.AnimalName.Should().Be("cat");
        enviada.Breed.Should().Be("Siamese");
        enviada.MedicalHistory.Should().Be("No previous conditions");
        enviada.Symptoms.Should().Equal("Sneezing", "Coughing");
    }

    [Theory]
    [InlineData("high", "Alta")]
    [InlineData("medium", "Media")]
    [InlineData("low", "Baixa")]
    public async Task AvaliarAsync_DeveConverterPrioridadeERetornarRecomendacao(string prioridadeIa, string esperada)
    {
        // Arrange
        PetCadastrado();
        IaRetorna(prioridadeIa, 0.8);

        // Act
        var resultado = await _service.AvaliarAsync(1, Requisicao(HistoricoMedico.Vacinado, "tosse"), Tutor);

        // Assert
        resultado.Prioridade.Should().Be(esperada);
        resultado.Confianca.Should().Be(0.8);
        resultado.Recomendacao.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AvaliarAsync_DeveExplicarOsFatoresQuePesaram()
    {
        // Arrange
        PetCadastrado(especie: "Cachorro", raca: "Labrador", idade: 11);
        IaRetorna("high", 0.88);

        // Act
        var resultado = await _service.AvaliarAsync(1,
            Requisicao(HistoricoMedico.DoencaCronica, "dificuldade para respirar", "febre", "espirros"), Tutor);

        // Assert
        resultado.Fatores.Should().Equal(
            "Sinal grave: dificuldade para respirar",
            "Sinal moderado: febre",
            "Pet sênior: 11 anos",
            "Histórico: doença crônica");
    }

    [Fact]
    public async Task AvaliarAsync_ComSinalGraveEPrioridadeBaixa_DeveIncluirAlertaDeSeguranca()
    {
        // Arrange
        PetCadastrado();
        IaRetorna("low", 0.7);

        // Act
        var resultado = await _service.AvaliarAsync(1, Requisicao(HistoricoMedico.Vacinado, "convulsão"), Tutor);

        // Assert
        resultado.Prioridade.Should().Be("Baixa");
        resultado.Recomendacao.Should().Contain("Atenção: convulsão é um sinal grave");
    }

    [Fact]
    public async Task AvaliarAsync_ComPetNaoVacinado_DeveRecomendarVacinacao()
    {
        // Arrange
        PetCadastrado();
        IaRetorna("low", 0.9);

        // Act
        var resultado = await _service.AvaliarAsync(1, Requisicao(HistoricoMedico.NaoVacinado, "espirros"), Tutor);

        // Assert
        resultado.Recomendacao.Should().Contain("vacinação");
        resultado.Fatores.Should().Contain("Histórico: vacinação não está em dia");
    }

    [Fact]
    public async Task AvaliarAsync_DeveGuardarAvaliacaoERegistrarNaAuditoria()
    {
        // Arrange
        PetCadastrado();
        IaRetorna("high", 0.79);

        // Act
        await _service.AvaliarAsync(1, Requisicao(HistoricoMedico.DoencaCronica, "febre", "vômito"), Tutor);

        // Assert
        _avaliacoesMock.Verify(a => a.AdicionarAsync(
            It.Is<AvaliacaoRisco>(av => av.PetId == 1 && av.Prioridade == "Alta" && av.Sintomas == "febre|vômito"),
            It.IsAny<CancellationToken>()), Times.Once);
        _atividadesMock.Verify(a => a.RegistrarAsync(
            It.Is<RegistroAtividade>(r => r.Acao == RegistroAtividade.AcaoAvaliacaoRisco),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AvaliarAsync_ComSintomaDesconhecido_DeveEnviarComoDigitadoEInformarNaResposta()
    {
        // Arrange
        PetCadastrado();
        IaRetorna("low", 0.9);

        // Act
        var resultado = await _service.AvaliarAsync(1, Requisicao(HistoricoMedico.Vacinado, "olhos lacrimejando"), Tutor);

        // Assert
        resultado.SintomasNaoReconhecidos.Should().Equal("olhos lacrimejando");
    }

    [Fact]
    public async Task AvaliarAsync_PorOutroTutor_DeveLancarAcessoNegadoSemChamarIa()
    {
        // Arrange
        PetCadastrado();
        var outroTutor = new UsuarioAtual(99, "Tutor");

        // Act
        var avaliar = () => _service.AvaliarAsync(1, Requisicao(HistoricoMedico.Vacinado, "febre"), outroTutor);

        // Assert
        await avaliar.Should().ThrowAsync<AcessoNegadoException>();
        _clyvoAiMock.Verify(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AvaliarAsync_ComPetQueNaoECaoNemGato_DeveLancarDomainExceptionSemChamarIa()
    {
        // Arrange
        PetCadastrado(especie: "Coelho", raca: "Mini Lop");

        // Act
        var avaliar = () => _service.AvaliarAsync(1, Requisicao(HistoricoMedico.Vacinado, "febre"), Tutor);

        // Assert
        await avaliar.Should().ThrowAsync<DomainException>();
        _clyvoAiMock.Verify(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AvaliarAsync_ComPetInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _petRepositoryMock.Setup(r => r.ObterPorIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        // Act
        var avaliar = () => _service.AvaliarAsync(9, Requisicao(HistoricoMedico.Vacinado, "febre"), Tutor);

        // Assert
        await avaliar.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AvaliarAsync_QuandoIaIndisponivel_NaoDeveGuardarNada()
    {
        // Arrange
        PetCadastrado();
        _clyvoAiMock.Setup(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServicoIndisponivelException("fora do ar"));

        // Act
        var avaliar = () => _service.AvaliarAsync(1, Requisicao(HistoricoMedico.Vacinado, "febre"), Tutor);

        // Assert
        await avaliar.Should().ThrowAsync<ServicoIndisponivelException>();
        _avaliacoesMock.Verify(a => a.AdicionarAsync(It.IsAny<AvaliacaoRisco>(), It.IsAny<CancellationToken>()), Times.Never);
        _atividadesMock.Verify(a => a.RegistrarAsync(It.IsAny<RegistroAtividade>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarPorPetAsync_DeveRetornarHistoricoConvertido()
    {
        // Arrange
        PetCadastrado();
        var filtro = new AvaliacaoFiltro();
        var avaliacao = new AvaliacaoRisco(1, "Media", 0.65, "DoencaRenalPrevia", ["perda de peso"], ["Histórico: doença renal prévia"], "Agende uma consulta.", UsuarioId);
        _avaliacoesMock.Setup(a => a.ListarPorPetAsync(1, filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AvaliacaoRisco>([avaliacao], 1, 1, 10));

        // Act
        var resultado = await _service.ListarPorPetAsync(1, filtro, Tutor);

        // Assert
        var dto = resultado.Itens.Should().ContainSingle().Subject;
        dto.Prioridade.Should().Be("Media");
        dto.Sintomas.Should().Equal("perda de peso");
        dto.Fatores.Should().Equal("Histórico: doença renal prévia");
    }

    [Fact]
    public async Task ListarPorPetAsync_PorOutroTutor_DeveLancarAcessoNegado()
    {
        // Arrange
        PetCadastrado();

        // Act
        var listar = () => _service.ListarPorPetAsync(1, new AvaliacaoFiltro(), new UsuarioAtual(99, "Tutor"));

        // Assert
        await listar.Should().ThrowAsync<AcessoNegadoException>();
    }
}
