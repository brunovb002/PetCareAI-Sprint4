using PetCareAI.Domain.Common;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Domain.Entities;

public class Consulta
{
    public const int VeterinarioMax = 120;
    public const int MotivoMax = 200;
    public const int DiagnosticoMax = 400;

    public int Id { get; private set; }
    public int PetId { get; private set; }
    public Pet? Pet { get; private set; }

    public DateTime DataConsulta { get; private set; }
    public string Veterinario { get; private set; } = string.Empty;
    public string Motivo { get; private set; } = string.Empty;
    public string Diagnostico { get; private set; } = string.Empty;

    // Usado pelo Entity Framework
    private Consulta() { }

    public Consulta(int petId, DateTime dataConsulta, string veterinario, string motivo, string? diagnostico)
    {
        if (petId <= 0)
            throw new DomainException("A consulta precisa estar vinculada a um pet válido.");

        PetId = petId;
        Atualizar(dataConsulta, veterinario, motivo, diagnostico);
    }

    public void Atualizar(DateTime dataConsulta, string veterinario, string motivo, string? diagnostico)
    {
        if (dataConsulta == default)
            throw new DomainException("Data da consulta é obrigatória.");

        // Valida tudo antes de alterar, para não deixar a consulta em estado parcial
        var veterinarioValido = Guard.TextoObrigatorio(veterinario, "Veterinário", VeterinarioMax);
        var motivoValido = Guard.TextoObrigatorio(motivo, "Motivo", MotivoMax);
        var diagnosticoValido = Guard.TextoOpcional(diagnostico, "Diagnóstico", DiagnosticoMax);

        DataConsulta = dataConsulta;
        Veterinario = veterinarioValido;
        Motivo = motivoValido;
        Diagnostico = diagnosticoValido;
    }
}
