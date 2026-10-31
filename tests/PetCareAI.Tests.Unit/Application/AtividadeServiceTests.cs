using FluentAssertions;
using Moq;
using PetCareAI.Application.Services;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Tests.Unit.Application;

public class AtividadeServiceTests
{
    [Fact]
    public async Task ListarAsync_DeveConverterRegistrosDoMongoParaDto()
    {
        // Arrange
        var repositoryMock = new Mock<IRegistroAtividadeRepository>();
        var filtro = new AtividadeFiltro { Entidade = "Pet" };
        var dataHora = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);
        var registro = new RegistroAtividade("Pet", "10", RegistroAtividade.AcaoCriacao, 1, "Pet 'Thor' cadastrado.", "abc123", dataHora);
        repositoryMock.Setup(r => r.ListarAsync(filtro, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<RegistroAtividade>([registro], 1, 1, 10));
        var service = new AtividadeService(repositoryMock.Object);

        // Act
        var resultado = await service.ListarAsync(filtro);

        // Assert
        var dto = resultado.Itens.Should().ContainSingle().Subject;
        dto.Id.Should().Be("abc123");
        dto.EntidadeId.Should().Be("10");
        dto.Acao.Should().Be(RegistroAtividade.AcaoCriacao);
        dto.DataHora.Should().Be(dataHora);
    }
}
