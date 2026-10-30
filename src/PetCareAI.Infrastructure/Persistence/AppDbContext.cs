using Microsoft.EntityFrameworkCore;
using PetCareAI.Domain.Entities;

namespace PetCareAI.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carrega as classes IEntityTypeConfiguration da pasta Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
