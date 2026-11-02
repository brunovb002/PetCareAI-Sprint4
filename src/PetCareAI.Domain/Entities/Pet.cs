using PetCareAI.Domain.Common;
using PetCareAI.Domain.Exceptions;

namespace PetCareAI.Domain.Entities;

public class Pet
{
    public const int NomeMax = 100;
    public const int EspecieMax = 50;
    public const int RacaMax = 80;

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Especie { get; private set; } = string.Empty;
    public string Raca { get; private set; } = string.Empty;
    public int Idade { get; private set; }
    public double Peso { get; private set; }
    public int UsuarioId { get; private set; }

    public ICollection<Consulta> Consultas { get; private set; } = new List<Consulta>();

    // Usado pelo Entity Framework
    private Pet() { }

    public Pet(string nome, string especie, string raca, int idade, double peso, int usuarioId)
    {
        if (usuarioId <= 0)
            throw new DomainException("O pet precisa estar vinculado a um tutor (usuário) válido.");

        UsuarioId = usuarioId;
        Atualizar(nome, especie, raca, idade, peso);
    }

    public void Atualizar(string nome, string especie, string raca, int idade, double peso)
    {
        if (idade is < 0 or > 40)
            throw new DomainException("Idade deve estar entre 0 e 40 anos.");
        if (peso is <= 0 or > 200)
            throw new DomainException("Peso deve ser maior que 0 e no máximo 200 kg.");

        // Valida tudo antes de alterar, para não deixar o pet em estado parcial
        var nomeValido = Guard.TextoObrigatorio(nome, "Nome", NomeMax);
        var especieValida = Guard.TextoObrigatorio(especie, "Espécie", EspecieMax);
        var racaValida = Guard.TextoObrigatorio(raca, "Raça", RacaMax);

        Nome = nomeValido;
        Especie = especieValida;
        Raca = racaValida;
        Idade = idade;
        Peso = peso;
    }
}
