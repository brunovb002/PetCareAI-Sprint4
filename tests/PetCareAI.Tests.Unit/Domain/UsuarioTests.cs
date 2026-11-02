using FluentAssertions;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Tests.Unit.Domain;

public class UsuarioTests
{
    [Fact]
    public void Criar_DeveNormalizarEmailEUsarPerfilTutorPorPadrao()
    {
        // Arrange + Act
        var usuario = new Usuario("Bruno", "  Bruno@PetCare.COM ", perfil: "");

        // Assert
        usuario.Email.Should().Be("bruno@petcare.com");
        usuario.Perfil.Should().Be(Usuario.PerfilTutor);
    }

    [Fact]
    public void Criar_ComPerfilVeterinario_DeveAceitarIgnorandoMaiusculas()
    {
        // Arrange + Act
        var usuario = new Usuario("Carla", "carla@vet.com", "veterinario");

        // Assert
        usuario.Perfil.Should().Be(Usuario.PerfilVeterinario);
    }

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("a@")]
    [InlineData("")]
    public void Criar_ComEmailInvalido_DeveLancarDomainException(string email)
    {
        // Arrange
        var criar = () => new Usuario("Bruno", email, Usuario.PerfilTutor);

        // Act + Assert
        criar.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_ComPerfilDesconhecido_DeveLancarDomainException()
    {
        // Arrange
        var criar = () => new Usuario("Bruno", "bruno@petcare.com", "Admin");

        // Act + Assert
        criar.Should().Throw<DomainException>();
    }

    [Fact]
    public void DefinirSenhaHash_Vazio_DeveLancarDomainException()
    {
        // Arrange
        var usuario = new Usuario("Bruno", "bruno@petcare.com", Usuario.PerfilTutor);

        // Act
        var definir = () => usuario.DefinirSenhaHash(" ");

        // Assert
        definir.Should().Throw<DomainException>();
    }
}
