using FluentAssertions;
using Moq;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Services;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _repositoryMock = new();
    private readonly Mock<ISenhaHasher> _hasherMock = new();
    private readonly Mock<ITokenService> _tokenMock = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_repositoryMock.Object, _hasherMock.Object, _tokenMock.Object);
    }

    private static Usuario UsuarioComSenha()
    {
        var usuario = new Usuario("Bruno", "bruno@petcare.com", Usuario.PerfilTutor);
        usuario.DefinirSenhaHash("hash-da-senha");
        return usuario;
    }

    [Fact]
    public async Task RegistrarAsync_ComEmailNovo_DeveSalvarComSenhaCriptografada()
    {
        // Arrange
        var dto = new RegistrarUsuarioDto { Nome = "Bruno", Email = "Bruno@PetCare.com", Senha = "Senha@123" };
        _repositoryMock.Setup(r => r.EmailExisteAsync("bruno@petcare.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasherMock.Setup(h => h.GerarHash(It.IsAny<Usuario>(), "Senha@123")).Returns("hash-gerado");
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario u, CancellationToken _) => u);

        // Act
        var resultado = await _service.RegistrarAsync(dto);

        // Assert
        resultado.Email.Should().Be("bruno@petcare.com");
        resultado.Perfil.Should().Be(Usuario.PerfilTutor);
        _repositoryMock.Verify(r => r.AdicionarAsync(
            It.Is<Usuario>(u => u.SenhaHash == "hash-gerado"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_ComEmailJaCadastrado_DeveLancarConflictException()
    {
        // Arrange
        var dto = new RegistrarUsuarioDto { Nome = "Bruno", Email = "bruno@petcare.com", Senha = "Senha@123" };
        _repositoryMock.Setup(r => r.EmailExisteAsync("bruno@petcare.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var registrar = () => _service.RegistrarAsync(dto);

        // Assert
        await registrar.Should().ThrowAsync<ConflictException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ComCredenciaisValidas_DeveRetornarToken()
    {
        // Arrange
        var usuario = UsuarioComSenha();
        var expiraEm = DateTime.UtcNow.AddHours(2);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("bruno@petcare.com", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.Verificar(usuario, "Senha@123")).Returns(true);
        _tokenMock.Setup(t => t.GerarToken(usuario)).Returns(new TokenGerado("jwt-token", expiraEm));

        // Act
        var resultado = await _service.LoginAsync(new LoginDto { Email = " BRUNO@petcare.com ", Senha = "Senha@123" });

        // Assert
        resultado.Token.Should().Be("jwt-token");
        resultado.Tipo.Should().Be("Bearer");
        resultado.ExpiraEm.Should().Be(expiraEm);
        resultado.Usuario.Email.Should().Be("bruno@petcare.com");
    }

    [Fact]
    public async Task LoginAsync_ComSenhaErrada_DeveLancarCredenciaisInvalidas()
    {
        // Arrange
        var usuario = UsuarioComSenha();
        _repositoryMock.Setup(r => r.ObterPorEmailAsync("bruno@petcare.com", It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.Verificar(usuario, "errada")).Returns(false);

        // Act
        var login = () => _service.LoginAsync(new LoginDto { Email = "bruno@petcare.com", Senha = "errada" });

        // Assert
        await login.Should().ThrowAsync<CredenciaisInvalidasException>();
        _tokenMock.Verify(t => t.GerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoUsuarioExiste_DeveRetornarDadosSemSenha()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(UsuarioComSenha());

        // Act
        var resultado = await _service.ObterPorIdAsync(1);

        // Assert
        resultado.Nome.Should().Be("Bruno");
        resultado.Email.Should().Be("bruno@petcare.com");
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoUsuarioNaoExiste_DeveLancarNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorIdAsync(50, It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        // Act
        var obter = () => _service.ObterPorIdAsync(50);

        // Assert
        await obter.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task LoginAsync_ComUsuarioInexistente_DeveLancarCredenciaisInvalidas()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        // Act
        var login = () => _service.LoginAsync(new LoginDto { Email = "ninguem@petcare.com", Senha = "x" });

        // Assert
        await login.Should().ThrowAsync<CredenciaisInvalidasException>();
    }
}
