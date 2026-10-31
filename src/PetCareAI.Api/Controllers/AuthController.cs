using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCareAI.Api.Extensions;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;

namespace PetCareAI.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Cadastra um novo usuário (tutor ou veterinário).</summary>
    [HttpPost("registrar")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDto dto, CancellationToken ct)
    {
        var usuario = await _authService.RegistrarAsync(dto, ct);
        return CreatedAtAction(nameof(Eu), null, usuario);
    }

    /// <summary>Autentica o usuário e devolve um token JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        return Ok(await _authService.LoginAsync(dto, ct));
    }

    /// <summary>Dados do usuário autenticado.</summary>
    [HttpGet("eu")]
    [Authorize]
    [ProducesResponseType(typeof(UsuarioResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Eu(CancellationToken ct)
    {
        return Ok(await _authService.ObterPorIdAsync(User.ObterUsuarioId(), ct));
    }
}
