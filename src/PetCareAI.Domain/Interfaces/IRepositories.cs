using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Domain.Interfaces;

public interface IPetRepository
{
    Task<Pet> AdicionarAsync(Pet pet, CancellationToken ct = default);
    Task<Pet?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<Pet>> ListarAsync(PetFiltro filtro, CancellationToken ct = default);
    Task<bool> ExisteAsync(int id, CancellationToken ct = default);
    Task AtualizarAsync(Pet pet, CancellationToken ct = default);
    Task RemoverAsync(Pet pet, CancellationToken ct = default);
}

public interface IConsultaRepository
{
    Task<Consulta> AdicionarAsync(Consulta consulta, CancellationToken ct = default);
    Task<Consulta?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<Consulta>> ListarAsync(ConsultaFiltro filtro, CancellationToken ct = default);
    Task AtualizarAsync(Consulta consulta, CancellationToken ct = default);
    Task RemoverAsync(Consulta consulta, CancellationToken ct = default);
}

public interface IUsuarioRepository
{
    Task<Usuario> AdicionarAsync(Usuario usuario, CancellationToken ct = default);
    Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExisteAsync(string email, CancellationToken ct = default);
}

/// <summary>Histórico de atividades (NoSQL / MongoDB).</summary>
public interface IRegistroAtividadeRepository
{
    Task RegistrarAsync(RegistroAtividade registro, CancellationToken ct = default);
    Task<PagedResult<RegistroAtividade>> ListarAsync(AtividadeFiltro filtro, CancellationToken ct = default);
}
