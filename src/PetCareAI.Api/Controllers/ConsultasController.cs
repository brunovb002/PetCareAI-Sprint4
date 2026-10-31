using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCareAI.Api.Extensions;
using PetCareAI.Api.Hateoas;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Domain.Common;

namespace PetCareAI.Api.Controllers;

[ApiController]
[Route("api/consultas")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ConsultasController : ControllerBase
{
    private readonly IConsultaService _consultaService;

    public ConsultasController(IConsultaService consultaService)
    {
        _consultaService = consultaService;
    }

    /// <summary>Lista consultas com paginação, filtros (petId, veterinario, dataInicio, dataFim) e ordenação (id, data, veterinario).</summary>
    [HttpGet(Name = nameof(ListarConsultas))]
    [ProducesResponseType(typeof(RecursoPaginado<ConsultaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarConsultas([FromQuery] ConsultaFiltro filtro, CancellationToken ct)
    {
        var pagina = await _consultaService.ListarAsync(filtro, ct);
        return Ok(pagina.ParaRecursoPaginado(c => LinksDaConsulta(Url, c), p => Url.Link(nameof(ListarConsultas), new
        {
            pagina = p,
            tamanhoPagina = filtro.TamanhoPagina,
            petId = filtro.PetId,
            veterinario = filtro.Veterinario,
            dataInicio = filtro.DataInicio?.ToString("O"),
            dataFim = filtro.DataFim?.ToString("O"),
            ordenarPor = filtro.OrdenarPor,
            direcao = filtro.Direcao
        })!));
    }

    /// <summary>Busca uma consulta pelo ID.</summary>
    [HttpGet("{id:int}", Name = nameof(ObterConsulta))]
    [ProducesResponseType(typeof(Recurso<ConsultaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterConsulta(int id, CancellationToken ct)
    {
        var consulta = await _consultaService.ObterPorIdAsync(id, ct);
        return Ok(new Recurso<ConsultaResponseDto>(consulta, LinksDaConsulta(Url, consulta)));
    }

    /// <summary>Agenda uma consulta para um pet existente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Recurso<ConsultaResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CriarConsulta([FromBody] CreateConsultaDto dto, CancellationToken ct)
    {
        var consulta = await _consultaService.CadastrarAsync(dto, User.ObterUsuarioId(), ct);
        return CreatedAtRoute(nameof(ObterConsulta), new { id = consulta.Id },
            new Recurso<ConsultaResponseDto>(consulta, LinksDaConsulta(Url, consulta)));
    }

    /// <summary>Atualiza uma consulta.</summary>
    [HttpPut("{id:int}", Name = nameof(AtualizarConsulta))]
    [ProducesResponseType(typeof(Recurso<ConsultaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarConsulta(int id, [FromBody] UpdateConsultaDto dto, CancellationToken ct)
    {
        var consulta = await _consultaService.AtualizarAsync(id, dto, User.ObterUsuarioId(), ct);
        return Ok(new Recurso<ConsultaResponseDto>(consulta, LinksDaConsulta(Url, consulta)));
    }

    /// <summary>Remove uma consulta.</summary>
    [HttpDelete("{id:int}", Name = nameof(RemoverConsulta))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverConsulta(int id, CancellationToken ct)
    {
        await _consultaService.RemoverAsync(id, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    internal static IReadOnlyList<LinkDto> LinksDaConsulta(IUrlHelper url, ConsultaResponseDto c) =>
    [
        new(url.Link(nameof(ObterConsulta), new { id = c.Id })!, "self", "GET"),
        new(url.Link(nameof(AtualizarConsulta), new { id = c.Id })!, "atualizar", "PUT"),
        new(url.Link(nameof(RemoverConsulta), new { id = c.Id })!, "remover", "DELETE"),
        new(url.Link(nameof(PetsController.ObterPet), new { id = c.PetId })!, "pet", "GET")
    ];
}
