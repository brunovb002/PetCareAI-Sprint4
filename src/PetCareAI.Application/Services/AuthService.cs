using PetCareAI.Application.DTOs;
using PetCareAI.Application.Interfaces;
using PetCareAI.Application.Mappings;
using PetCareAI.Domain.Entities;
using PetCareAI.Domain.Exceptions;
using PetCareAI.Domain.Interfaces;

namespace PetCareAI.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _repository;
    private readonly ISenhaHasher _hasher;
    private readonly ITokenService _tokenService;

    public AuthService(IUsuarioRepository repository, ISenhaHasher hasher, ITokenService tokenService)
    {
        _repository = repository;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<UsuarioResponseDto> RegistrarAsync(RegistrarUsuarioDto dto, CancellationToken ct = default)
    {
        var usuario = new Usuario(dto.Nome, dto.Email, dto.Perfil ?? Usuario.PerfilTutor);

        if (await _repository.EmailExisteAsync(usuario.Email, ct))
            throw new ConflictException("Já existe um usuário cadastrado com este e-mail.");

        usuario.DefinirSenhaHash(_hasher.GerarHash(usuario, dto.Senha));
        var criado = await _repository.AdicionarAsync(usuario, ct);
        return criado.ToDto();
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var usuario = await _repository.ObterPorEmailAsync(dto.Email.Trim().ToLowerInvariant(), ct);
        if (usuario is null || !_hasher.Verificar(usuario, dto.Senha))
            throw new CredenciaisInvalidasException();

        var token = _tokenService.GerarToken(usuario);
        return new TokenResponseDto
        {
            Token = token.Token,
            ExpiraEm = token.ExpiraEm,
            Usuario = usuario.ToDto()
        };
    }

    public async Task<UsuarioResponseDto> ObterPorIdAsync(int id, CancellationToken ct = default)
    {
        var usuario = await _repository.ObterPorIdAsync(id, ct) ?? throw new NotFoundException(nameof(Usuario), id);
        return usuario.ToDto();
    }
}
