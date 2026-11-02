namespace PetCareAI.Application.DTOs;

public class AtividadeResponseDto
{
    public string? Id { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public string EntidadeId { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
}
