using PetCareAI.Application.DTOs;
using PetCareAI.Domain.Common;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Application.Interfaces;

public interface IPetService
{
    Task<PetResponseDto> CadastrarAsync(CreatePetDto dto, int usuarioId, CancellationToken ct = default);
    Task<PetResponseDto> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<PetResponseDto>> ListarAsync(PetFiltro filtro, CancellationToken ct = default);
    Task<PetResponseDto> AtualizarAsync(int id, UpdatePetDto dto, int usuarioId, CancellationToken ct = default);
    Task RemoverAsync(int id, int usuarioId, CancellationToken ct = default);
}

public interface IConsultaService
{
    Task<ConsultaResponseDto> CadastrarAsync(CreateConsultaDto dto, int usuarioId, CancellationToken ct = default);
    Task<ConsultaResponseDto> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<ConsultaResponseDto>> ListarAsync(ConsultaFiltro filtro, CancellationToken ct = default);
    Task<ConsultaResponseDto> AtualizarAsync(int id, UpdateConsultaDto dto, int usuarioId, CancellationToken ct = default);
    Task RemoverAsync(int id, int usuarioId, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<UsuarioResponseDto> RegistrarAsync(RegistrarUsuarioDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<UsuarioResponseDto> ObterPorIdAsync(int id, CancellationToken ct = default);
}

public interface IAtividadeService
{
    Task<PagedResult<AtividadeResponseDto>> ListarAsync(AtividadeFiltro filtro, CancellationToken ct = default);
}

public interface IAvaliacaoRiscoService
{
    Task<AvaliacaoRiscoResponseDto> AvaliarAsync(int petId, AvaliacaoRiscoRequestDto dto, int usuarioId, CancellationToken ct = default);
}

// ---- Portas implementadas pela Infrastructure ----

/// <summary>Cliente do serviço de IA CLYVO AI (modelo Random Forest em Python).</summary>
public interface IClyvoAiClient
{
    Task<PrevisaoClyvoAi> PreverAsync(EntradaClyvoAi entrada, CancellationToken ct = default);
}

public interface ISenhaHasher
{
    string GerarHash(Usuario usuario, string senha);
    bool Verificar(Usuario usuario, string senha);
}

public interface ITokenService
{
    TokenGerado GerarToken(Usuario usuario);
}
