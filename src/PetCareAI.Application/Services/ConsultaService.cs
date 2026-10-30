using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Mappings;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Application.Services;

public class ConsultaService : IConsultaService
{
    private const string Entidade = nameof(Consulta);

    private readonly IConsultaRepository _repository;
    private readonly IPetRepository _petRepository;
    private readonly IRegistroAtividadeRepository _atividades;

    public ConsultaService(IConsultaRepository repository, IPetRepository petRepository, IRegistroAtividadeRepository atividades)
    {
        _repository = repository;
        _petRepository = petRepository;
        _atividades = atividades;
    }

    public async Task<ConsultaResponseDto> CadastrarAsync(CreateConsultaDto dto, int usuarioId, CancellationToken ct = default)
    {
        if (!await _petRepository.ExisteAsync(dto.PetId, ct))
            throw new NotFoundException(nameof(Pet), dto.PetId);

        var consulta = new Consulta(dto.PetId, dto.DataConsulta, dto.Veterinario, dto.Motivo, dto.Diagnostico);
        var criada = await _repository.AdicionarAsync(consulta, ct);

        await RegistrarAsync(criada, RegistroAtividade.AcaoCriacao, usuarioId,
            $"Consulta do pet {criada.PetId} agendada com {criada.Veterinario}.", ct);
        return criada.ToDto();
    }

    public async Task<ConsultaResponseDto> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var consulta = await ObterOuFalharAsync(id, ct);
        return consulta.ToDto();
    }

    public async Task<PagedResult<ConsultaResponseDto>> ListarAsync(ConsultaFiltro filtro, CancellationToken ct = default)
    {
        if (filtro.DataInicio > filtro.DataFim)
            throw new DomainException("DataInicio não pode ser maior que DataFim.");

        var pagina = await _repository.ListarAsync(filtro, ct);
        return pagina.Map(c => c.ToDto());
    }

    public async Task<ConsultaResponseDto> AtualizarAsync(int id, UpdateConsultaDto dto, int usuarioId, CancellationToken ct = default)
    {
        var consulta = await ObterOuFalharAsync(id, ct);
        consulta.Atualizar(dto.DataConsulta, dto.Veterinario, dto.Motivo, dto.Diagnostico);
        await _repository.AtualizarAsync(consulta, ct);

        await RegistrarAsync(consulta, RegistroAtividade.AcaoAtualizacao, usuarioId,
            $"Consulta {consulta.Id} atualizada.", ct);
        return consulta.ToDto();
    }

    public async Task RemoverAsync(int id, int usuarioId, CancellationToken ct = default)
    {
        var consulta = await ObterOuFalharAsync(id, ct);
        await _repository.RemoverAsync(consulta, ct);

        await RegistrarAsync(consulta, RegistroAtividade.AcaoRemocao, usuarioId,
            $"Consulta {consulta.Id} removida.", ct);
    }

    private async Task<Consulta> ObterOuFalharAsync(int id, CancellationToken ct) =>
        await _repository.ObterPorIdAsync(id, ct) ?? throw new NotFoundException(Entidade, id);

    private Task RegistrarAsync(Consulta consulta, string acao, int usuarioId, string descricao, CancellationToken ct) =>
        _atividades.RegistrarAsync(new RegistroAtividade(Entidade, consulta.Id.ToString(), acao, usuarioId, descricao), ct);
}
