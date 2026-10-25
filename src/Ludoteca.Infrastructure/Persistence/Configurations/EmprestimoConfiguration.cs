using Ludoteca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ludoteca.Infrastructure.Persistence.Configurations;

public class EmprestimoConfiguration : IEntityTypeConfiguration<Emprestimo>
{
    public void Configure(EntityTypeBuilder<Emprestimo> builder)
    {
        builder.ToTable("LUDO_EMPRESTIMO");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("EMPRESTIMO_ID").ValueGeneratedOnAdd();

        builder.Property(e => e.JogoId).HasColumnName("JOGO_ID").IsRequired();
        builder.Property(e => e.MembroId).HasColumnName("MEMBRO_ID").IsRequired();
        builder.Property(e => e.DataEmprestimo).HasColumnName("DATA_EMPRESTIMO").IsRequired();
        builder.Property(e => e.DataPrevistaDevolucao).HasColumnName("DATA_PREVISTA_DEVOLUCAO").IsRequired();
        builder.Property(e => e.DataDevolucao).HasColumnName("DATA_DEVOLUCAO");

        builder.Ignore(e => e.Ativo);

        builder.HasOne(e => e.Jogo)
            .WithMany()
            .HasForeignKey(e => e.JogoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Membro)
            .WithMany()
            .HasForeignKey(e => e.MembroId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.MembroId, e.DataDevolucao }).HasDatabaseName("IX_LUDO_EMPRESTIMO_MEMBRO_DEV");
        builder.HasIndex(e => new { e.JogoId, e.DataDevolucao }).HasDatabaseName("IX_LUDO_EMPRESTIMO_JOGO_DEV");
    }
}
