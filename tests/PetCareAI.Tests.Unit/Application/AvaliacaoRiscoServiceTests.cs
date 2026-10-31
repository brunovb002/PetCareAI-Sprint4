using FluentAssertions;
using Moq;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Exceptions;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Services;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Tests.Unit.Application;

public class AvaliacaoRiscoServiceTests
{
    private const int UsuarioId = 1;

    private readonly Mock<IPetRepository> _petRepositoryMock = new();
    private readonly Mock<IClyvoAiClient> _clyvoAiMock = new();
    private readonly Mock<IRegistroAtividadeRepository> _atividadesMock = new();
    private readonly AvaliacaoRiscoService _service;

    public AvaliacaoRiscoServiceTests()
    {
        _service = new AvaliacaoRiscoService(_petRepositoryMock.Object, _clyvoAiMock.Object, _atividadesMock.Object);
    }

    private void PetCadastrado(string especie = "Gato", string raca = "Siamês") =>
        _petRepositoryMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pet("Mimi", especie, raca, 2, 4.2, UsuarioId));

    private void IaRetorna(string prioridade, double confianca) =>
        _clyvoAiMock.Setup(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrevisaoClyvoAi(prioridade, new Dictionary<string, double> { [prioridade] = confianca }));

    [Fact]
    public async Task AvaliarAsync_DeveTraduzirDadosDoPetESintomasParaOFormatoDoModelo()
    {
        // Arrange
        PetCadastrado();
        EntradaClyvoAi? enviada = null;
        _clyvoAiMock.Setup(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()))
            .Callback((EntradaClyvoAi e, CancellationToken _) => enviada = e)
            .ReturnsAsync(new PrevisaoClyvoAi("low", new Dictionary<string, double> { ["low"] = 0.997 }));
        var dto = new AvaliacaoRiscoRequestDto
        {
            HistoricoMedico = HistoricoMedico.SemCondicoesPrevias,
            Sintomas = ["Espirros", "tosse leve"]
        };

        // Act
        await _service.AvaliarAsync(1, dto, UsuarioId);

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
        var dto = new AvaliacaoRiscoRequestDto { HistoricoMedico = HistoricoMedico.Vacinado, Sintomas = ["febre"] };

        // Act
        var resultado = await _service.AvaliarAsync(1, dto, UsuarioId);

        // Assert
        resultado.Prioridade.Should().Be(esperada);
        resultado.Confianca.Should().Be(0.8);
        resultado.Recomendacao.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AvaliarAsync_DeveRegistrarAvaliacaoNoHistorico()
    {
        // Arrange
        PetCadastrado();
        IaRetorna("high", 0.79);
        var dto = new AvaliacaoRiscoRequestDto { HistoricoMedico = HistoricoMedico.DoencaCronica, Sintomas = ["convulsão"] };

        // Act
        await _service.AvaliarAsync(1, dto, UsuarioId);

        // Assert
        _atividadesMock.Verify(a => a.RegistrarAsync(
            It.Is<RegistroAtividade>(r => r.Acao == RegistroAtividade.AcaoAvaliacaoRisco && r.Descricao.Contains("Alta")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AvaliarAsync_ComSintomaDesconhecido_DeveEnviarComoDigitadoEInformarNaResposta()
    {
        // Arrange
        PetCadastrado();
        IaRetorna("low", 0.9);
        var dto = new AvaliacaoRiscoRequestDto { HistoricoMedico = HistoricoMedico.Vacinado, Sintomas = ["olhos lacrimejando"] };

        // Act
        var resultado = await _service.AvaliarAsync(1, dto, UsuarioId);

        // Assert
        resultado.SintomasNaoReconhecidos.Should().Equal("olhos lacrimejando");
    }

    [Fact]
    public async Task AvaliarAsync_ComPetQueNaoECaoNemGato_DeveLancarDomainExceptionSemChamarIa()
    {
        // Arrange
        PetCadastrado(especie: "Coelho", raca: "Mini Lop");
        var dto = new AvaliacaoRiscoRequestDto { HistoricoMedico = HistoricoMedico.Vacinado, Sintomas = ["febre"] };

        // Act
        var avaliar = () => _service.AvaliarAsync(1, dto, UsuarioId);

        // Assert
        await avaliar.Should().ThrowAsync<DomainException>();
        _clyvoAiMock.Verify(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AvaliarAsync_ComPetInexistente_DeveLancarNotFoundException()
    {
        // Arrange
        _petRepositoryMock.Setup(r => r.ObterPorIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);
        var dto = new AvaliacaoRiscoRequestDto { HistoricoMedico = HistoricoMedico.Vacinado, Sintomas = ["febre"] };

        // Act
        var avaliar = () => _service.AvaliarAsync(9, dto, UsuarioId);

        // Assert
        await avaliar.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AvaliarAsync_QuandoIaIndisponivel_NaoDeveRegistrarAtividade()
    {
        // Arrange
        PetCadastrado();
        _clyvoAiMock.Setup(c => c.PreverAsync(It.IsAny<EntradaClyvoAi>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServicoIndisponivelException("fora do ar"));
        var dto = new AvaliacaoRiscoRequestDto { HistoricoMedico = HistoricoMedico.Vacinado, Sintomas = ["febre"] };

        // Act
        var avaliar = () => _service.AvaliarAsync(1, dto, UsuarioId);

        // Assert
        await avaliar.Should().ThrowAsync<ServicoIndisponivelException>();
        _atividadesMock.Verify(a => a.RegistrarAsync(It.IsAny<RegistroAtividade>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
