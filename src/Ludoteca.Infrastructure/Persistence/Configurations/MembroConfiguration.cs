using Ludoteca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ludoteca.Infrastructure.Persistence.Configurations;

public class MembroConfiguration : IEntityTypeConfiguration<Membro>
{
    public void Configure(EntityTypeBuilder<Membro> builder)
    {
        builder.ToTable("LUDO_MEMBRO");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("MEMBRO_ID").ValueGeneratedOnAdd();

        builder.Property(m => m.Nome).HasColumnName("NOME").HasMaxLength(120).IsRequired();
        builder.Property(m => m.Email).HasColumnName("EMAIL").HasMaxLength(160).IsRequired();
        builder.Property(m => m.Telefone).HasColumnName("TELEFONE").HasMaxLength(25);
        builder.Property(m => m.Cep).HasColumnName("CEP").HasMaxLength(8).IsRequired();
        builder.Property(m => m.Logradouro).HasColumnName("LOGRADOURO").HasMaxLength(160);
        builder.Property(m => m.Bairro).HasColumnName("BAIRRO").HasMaxLength(100);
        builder.Property(m => m.Cidade).HasColumnName("CIDADE").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Uf).HasColumnName("UF").HasMaxLength(2).IsRequired();
        builder.Property(m => m.CriadoEm).HasColumnName("CRIADO_EM").IsRequired();

        builder.HasIndex(m => m.Email).IsUnique().HasDatabaseName("UX_LUDO_MEMBRO_EMAIL");
        builder.HasIndex(m => m.Nome).HasDatabaseName("IX_LUDO_MEMBRO_NOME");
    }
}
