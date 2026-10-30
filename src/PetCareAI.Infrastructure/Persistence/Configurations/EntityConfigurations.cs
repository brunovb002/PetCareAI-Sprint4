using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Infrastructure.Persistence.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(Pet.NomeMax).IsRequired();
        builder.Property(p => p.Especie).HasMaxLength(Pet.EspecieMax).IsRequired();
        builder.Property(p => p.Raca).HasMaxLength(Pet.RacaMax).IsRequired();
        builder.HasIndex(p => p.Especie);
    }
}

public class ConsultaConfiguration : IEntityTypeConfiguration<Consulta>
{
    public void Configure(EntityTypeBuilder<Consulta> builder)
    {
        builder.ToTable("Consultas");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Veterinario).HasMaxLength(Consulta.VeterinarioMax).IsRequired();
        builder.Property(c => c.Motivo).HasMaxLength(Consulta.MotivoMax).IsRequired();
        builder.Property(c => c.Diagnostico).HasMaxLength(Consulta.DiagnosticoMax).IsRequired();

        builder.HasOne(c => c.Pet)
            .WithMany(p => p.Consultas)
            .HasForeignKey(c => c.PetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Nome).HasMaxLength(Usuario.NomeMax).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(Usuario.EmailMax).IsRequired();
        builder.Property(u => u.SenhaHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Perfil).HasMaxLength(20).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
