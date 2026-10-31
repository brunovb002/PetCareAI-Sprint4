using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCareAI.Api.Extensions;
using PetCareAI.Api.Hateoas;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Domain.Common;

namespace PetCareAI.Api.Controllers;

[ApiController]
[Route("api/pets")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class PetsController : ControllerBase
{
    private readonly IPetService _petService;
    private readonly IConsultaService _consultaService;
    private readonly IAvaliacaoRiscoService _avaliacaoRiscoService;

    public PetsController(IPetService petService, IConsultaService consultaService, IAvaliacaoRiscoService avaliacaoRiscoService)
    {
        _petService = petService;
        _consultaService = consultaService;
        _avaliacaoRiscoService = avaliacaoRiscoService;
    }

    /// <summary>Lista pets com paginação, filtros (nome, especie, usuarioId) e ordenação (id, nome, especie, idade, peso).</summary>
    [HttpGet(Name = nameof(ListarPets))]
    [ProducesResponseType(typeof(RecursoPaginado<PetResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarPets([FromQuery] PetFiltro filtro, CancellationToken ct)
    {
        var pagina = await _petService.ListarAsync(filtro, ct);
        return Ok(pagina.ParaRecursoPaginado(LinksDoPet, p => Url.Link(nameof(ListarPets), new
        {
            pagina = p,
            tamanhoPagina = filtro.TamanhoPagina,
            nome = filtro.Nome,
            especie = filtro.Especie,
            usuarioId = filtro.UsuarioId,
            ordenarPor = filtro.OrdenarPor,
            direcao = filtro.Direcao
        })!));
    }

    /// <summary>Busca um pet pelo ID.</summary>
    [HttpGet("{id:int}", Name = nameof(ObterPet))]
    [ProducesResponseType(typeof(Recurso<PetResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPet(int id, CancellationToken ct)
    {
        var pet = await _petService.ObterPorIdAsync(id, ct);
        return Ok(new Recurso<PetResponseDto>(pet, LinksDoPet(pet)));
    }

    /// <summary>Cadastra um pet vinculado ao usuário autenticado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Recurso<PetResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarPet([FromBody] CreatePetDto dto, CancellationToken ct)
    {
        var pet = await _petService.CadastrarAsync(dto, User.ObterUsuarioId(), ct);
        return CreatedAtRoute(nameof(ObterPet), new { id = pet.Id }, new Recurso<PetResponseDto>(pet, LinksDoPet(pet)));
    }

    /// <summary>Atualiza os dados de um pet.</summary>
    [HttpPut("{id:int}", Name = nameof(AtualizarPet))]
    [ProducesResponseType(typeof(Recurso<PetResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarPet(int id, [FromBody] UpdatePetDto dto, CancellationToken ct)
    {
        var pet = await _petService.AtualizarAsync(id, dto, User.ObterUsuarioId(), ct);
        return Ok(new Recurso<PetResponseDto>(pet, LinksDoPet(pet)));
    }

    /// <summary>Remove um pet (e suas consultas).</summary>
    [HttpDelete("{id:int}", Name = nameof(RemoverPet))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverPet(int id, CancellationToken ct)
    {
        await _petService.RemoverAsync(id, User.ObterUsuarioId(), ct);
        return NoContent();
    }

    /// <summary>Lista as consultas de um pet (paginado).</summary>
    [HttpGet("{id:int}/consultas", Name = nameof(ListarConsultasDoPet))]
    [ProducesResponseType(typeof(RecursoPaginado<ConsultaResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarConsultasDoPet(int id, [FromQuery] int pagina = 1, [FromQuery] int tamanhoPagina = 10,
        CancellationToken ct = default)
    {
        await _petService.ObterPorIdAsync(id, ct); // 404 se o pet não existir

        var filtro = new ConsultaFiltro { PetId = id, Pagina = pagina, TamanhoPagina = tamanhoPagina, OrdenarPor = "data", Direcao = "desc" };
        var resultado = await _consultaService.ListarAsync(filtro, ct);

        return Ok(resultado.ParaRecursoPaginado(
            c => ConsultasController.LinksDaConsulta(Url, c),
            p => Url.Link(nameof(ListarConsultasDoPet), new { id, pagina = p, tamanhoPagina = filtro.TamanhoPagina })!));
    }

    /// <summary>
    /// CLYVO AI: avalia a prioridade de cuidado do pet (Alta, Media, Baixa) a partir dos dados cadastrados
    /// e dos sintomas relatados pelo tutor. O resultado fica registrado no histórico de atividades.
    /// </summary>
    [HttpPost("{id:int}/avaliacao-risco", Name = nameof(AvaliarRisco))]
    [ProducesResponseType(typeof(Recurso<AvaliacaoRiscoResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AvaliarRisco(int id, [FromBody] AvaliacaoRiscoRequestDto dto, CancellationToken ct)
    {
        var avaliacao = await _avaliacaoRiscoService.AvaliarAsync(id, dto, User.ObterUsuarioId(), ct);
        return Ok(new Recurso<AvaliacaoRiscoResponseDto>(avaliacao,
        [
            new(Url.Link(nameof(ObterPet), new { id })!, "pet", "GET"),
            new(Url.Link(nameof(ListarConsultasDoPet), new { id })!, "consultas", "GET"),
            new(Url.Link(nameof(AtividadesController.ListarAtividades), new { entidade = "Pet", entidadeId = id })!, "historico", "GET")
        ]));
    }

    private IReadOnlyList<LinkDto> LinksDoPet(PetResponseDto pet) =>
    [
        new(Url.Link(nameof(ObterPet), new { id = pet.Id })!, "self", "GET"),
        new(Url.Link(nameof(AtualizarPet), new { id = pet.Id })!, "atualizar", "PUT"),
        new(Url.Link(nameof(RemoverPet), new { id = pet.Id })!, "remover", "DELETE"),
        new(Url.Link(nameof(ListarConsultasDoPet), new { id = pet.Id })!, "consultas", "GET"),
        new(Url.Link(nameof(AvaliarRisco), new { id = pet.Id })!, "avaliacao-risco", "POST")
    ];
}
