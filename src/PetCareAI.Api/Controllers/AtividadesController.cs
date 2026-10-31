using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetCareAI.Api.Hateoas;
using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Domain.Common;

namespace PetCareAI.Api.Controllers;

/// <summary>Histórico de atividades (auditoria) gravado no MongoDB a cada criação, atualização ou remoção.</summary>
[ApiController]
[Route("api/atividades")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class AtividadesController : ControllerBase
{
    private readonly IAtividadeService _atividadeService;

    public AtividadesController(IAtividadeService atividadeService)
    {
        _atividadeService = atividadeService;
    }

    /// <summary>Lista o histórico (mais recente primeiro), com filtros por entidade (Pet, Consulta), acao (Criacao, Atualizacao, Remocao) e entidadeId.</summary>
    [HttpGet(Name = nameof(ListarAtividades))]
    [ProducesResponseType(typeof(RecursoPaginado<AtividadeResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAtividades([FromQuery] AtividadeFiltro filtro, CancellationToken ct)
    {
        var pagina = await _atividadeService.ListarAsync(filtro, ct);
        return Ok(pagina.ParaRecursoPaginado(LinksDaAtividade, p => Url.Link(nameof(ListarAtividades), new
        {
            pagina = p,
            tamanhoPagina = filtro.TamanhoPagina,
            entidade = filtro.Entidade,
            acao = filtro.Acao,
            entidadeId = filtro.EntidadeId
        })!));
    }

    private IReadOnlyList<LinkDto> LinksDaAtividade(AtividadeResponseDto a)
    {
        // Aponta para o recurso afetado (exceto quando ele foi removido)
        if (a.Acao == Domain.Entities.RegistroAtividade.AcaoRemocao || !int.TryParse(a.EntidadeId, out var id))
            return [];

        var rota = a.Entidade switch
        {
            nameof(Domain.Entities.Pet) => nameof(PetsController.ObterPet),
            nameof(Domain.Entities.Consulta) => nameof(ConsultasController.ObterConsulta),
            _ => null
        };
        return rota is null ? [] : [new LinkDto(Url.Link(rota, new { id })!, "recurso", "GET")];
    }
}
