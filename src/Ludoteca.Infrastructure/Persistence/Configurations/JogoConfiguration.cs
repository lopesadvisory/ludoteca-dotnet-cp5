using Ludoteca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ludoteca.Infrastructure.Persistence.Configurations;

public class JogoConfiguration : IEntityTypeConfiguration<Jogo>
{
    public void Configure(EntityTypeBuilder<Jogo> builder)
    {
        builder.ToTable("LUDO_JOGO");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("JOGO_ID").ValueGeneratedOnAdd();

        builder.Property(j => j.Titulo).HasColumnName("TITULO").HasMaxLength(120).IsRequired();
        builder.Property(j => j.Editora).HasColumnName("EDITORA").HasMaxLength(80).IsRequired();
        builder.Property(j => j.Categoria).HasColumnName("CATEGORIA").HasMaxLength(40).IsRequired();
        builder.Property(j => j.MinJogadores).HasColumnName("MIN_JOGADORES").IsRequired();
        builder.Property(j => j.MaxJogadores).HasColumnName("MAX_JOGADORES").IsRequired();
        builder.Property(j => j.DuracaoMediaMinutos).HasColumnName("DURACAO_MEDIA_MIN").IsRequired();
        builder.Property(j => j.CriadoEm).HasColumnName("CRIADO_EM").IsRequired();

        // Evita dois empréstimos simultâneos do mesmo jogo: o segundo UPDATE falha.
        builder.Property(j => j.Disponivel).HasColumnName("DISPONIVEL").IsRequired().IsConcurrencyToken();

        builder.HasIndex(j => j.Titulo).IsUnique().HasDatabaseName("UX_LUDO_JOGO_TITULO");
        builder.HasIndex(j => j.Categoria).HasDatabaseName("IX_LUDO_JOGO_CATEGORIA");
    }
}
