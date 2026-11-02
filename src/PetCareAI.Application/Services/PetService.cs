using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Mappings;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Application.Services;

public class PetService : IPetService
{
    private const string Entidade = nameof(Pet);

    private readonly IPetRepository _repository;
    private readonly IRegistroAtividadeRepository _atividades;

    public PetService(IPetRepository repository, IRegistroAtividadeRepository atividades)
    {
        _repository = repository;
        _atividades = atividades;
    }

    public async Task<PetResponseDto> CadastrarAsync(CreatePetDto dto, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var pet = new Pet(dto.Nome, dto.Especie, dto.Raca, dto.Idade, dto.Peso, usuario.Id);
        var criado = await _repository.AdicionarAsync(pet, ct);

        await RegistrarAsync(criado, RegistroAtividade.AcaoCriacao, usuario, $"Pet '{criado.Nome}' cadastrado.", ct);
        return criado.ToDto();
    }

    public async Task<PetResponseDto> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var pet = await ObterOuFalharAsync(id, ct);
        return pet.ToDto();
    }

    public async Task<PagedResult<PetResponseDto>> ListarAsync(PetFiltro filtro, CancellationToken ct = default)
    {
        var pagina = await _repository.ListarAsync(filtro, ct);
        return pagina.Map(p => p.ToDto());
    }

    public async Task<PetResponseDto> AtualizarAsync(int id, UpdatePetDto dto, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var pet = await ObterOuFalharAsync(id, ct);
        usuario.GarantirAcessoAoPet(pet);

        pet.Atualizar(dto.Nome, dto.Especie, dto.Raca, dto.Idade, dto.Peso);
        await _repository.AtualizarAsync(pet, ct);

        await RegistrarAsync(pet, RegistroAtividade.AcaoAtualizacao, usuario, $"Pet '{pet.Nome}' atualizado.", ct);
        return pet.ToDto();
    }

    public async Task RemoverAsync(int id, UsuarioAtual usuario, CancellationToken ct = default)
    {
        var pet = await ObterOuFalharAsync(id, ct);
        usuario.GarantirAcessoAoPet(pet);

        await _repository.RemoverAsync(pet, ct);

        await RegistrarAsync(pet, RegistroAtividade.AcaoRemocao, usuario, $"Pet '{pet.Nome}' removido.", ct);
    }

    private async Task<Pet> ObterOuFalharAsync(int id, CancellationToken ct) =>
        await _repository.ObterPorIdAsync(id, ct) ?? throw new NotFoundException(Entidade, id);

    private Task RegistrarAsync(Pet pet, string acao, UsuarioAtual usuario, string descricao, CancellationToken ct) =>
        _atividades.RegistrarAsync(new RegistroAtividade(Entidade, pet.Id.ToString(), acao, usuario.Id, descricao), ct);
}
