using Ludoteca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ludoteca.Infrastructure.Persistence;

public class LudotecaDbContext : DbContext
{
    public LudotecaDbContext(DbContextOptions<LudotecaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Jogo> Jogos => Set<Jogo>();

    public DbSet<Membro> Membros => Set<Membro>();

    public DbSet<Emprestimo> Emprestimos => Set<Emprestimo>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O Oracle devolve TIMESTAMP sem informação de fuso; as datas são gravadas em UTC e voltam marcadas como UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<DataEmUtcConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<DataEmUtcNulavelConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LudotecaDbContext).Assembly);
    }

    private sealed class DataEmUtcConverter : ValueConverter<DateTime, DateTime>
    {
        public DataEmUtcConverter()
            : base(data => data, data => DateTime.SpecifyKind(data, DateTimeKind.Utc))
        {
        }
    }

    private sealed class DataEmUtcNulavelConverter : ValueConverter<DateTime?, DateTime?>
    {
        public DataEmUtcNulavelConverter()
            : base(
                data => data,
                data => data.HasValue ? DateTime.SpecifyKind(data.Value, DateTimeKind.Utc) : data)
        {
        }
    }
}
