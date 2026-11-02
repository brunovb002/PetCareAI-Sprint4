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

    public async Task<ConsultaResponseDto> CadastrarAsync(CreateConsultaDto dto, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var pet = await ObterPetOuFalharAsync(dto.PetId, ct);
        usuario.GarantirAcessoAoPet(pet);

        var consulta = new Consulta(dto.PetId, dto.DataConsulta, dto.Veterinario, dto.Motivo, dto.Diagnostico);
        var criada = await _repository.AdicionarAsync(consulta, ct);

        await RegistrarAsync(criada, RegistroAtividade.AcaoCriacao, usuario,
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

    public async Task<ConsultaResponseDto> AtualizarAsync(int id, UpdateConsultaDto dto, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var consulta = await ObterOuFalharAsync(id, ct);
        usuario.GarantirAcessoAoPet(await ObterPetOuFalharAsync(consulta.PetId, ct));

        consulta.Atualizar(dto.DataConsulta, dto.Veterinario, dto.Motivo, dto.Diagnostico);
        await _repository.AtualizarAsync(consulta, ct);

        await RegistrarAsync(consulta, RegistroAtividade.AcaoAtualizacao, usuario,
            $"Consulta {consulta.Id} atualizada.", ct);
        return consulta.ToDto();
    }

    public async Task RemoverAsync(int id, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var consulta = await ObterOuFalharAsync(id, ct);
        usuario.GarantirAcessoAoPet(await ObterPetOuFalharAsync(consulta.PetId, ct));

        await _repository.RemoverAsync(consulta, ct);

        await RegistrarAsync(consulta, RegistroAtividade.AcaoRemocao, usuario,
            $"Consulta {consulta.Id} removida.", ct);
    }

    private async Task<Consulta> ObterOuFalharAsync(int id, CancellationToken ct) =>
        await _repository.ObterPorIdAsync(id, ct) ?? throw new NotFoundException(Entidade, id);

    private async Task<Pet> ObterPetOuFalharAsync(int petId, CancellationToken ct) =>
        await _petRepository.ObterPorIdAsync(petId, ct) ?? throw new NotFoundException(nameof(Pet), petId);

    private Task RegistrarAsync(Consulta consulta, string acao, UsuarioAtual usuario, string descricao, CancellationToken ct) =>
        _atividades.RegistrarAsync(new RegistroAtividade(Entidade, consulta.Id.ToString(), acao, usuario.Id, descricao), ct);
}
