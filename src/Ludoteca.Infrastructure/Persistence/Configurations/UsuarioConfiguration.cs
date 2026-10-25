using Ludoteca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ludoteca.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("LUDO_USUARIO");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("USUARIO_ID").ValueGeneratedOnAdd();

        builder.Property(u => u.Nome).HasColumnName("NOME").HasMaxLength(120).IsRequired();
        builder.Property(u => u.Email).HasColumnName("EMAIL").HasMaxLength(160).IsRequired();
        builder.Property(u => u.SenhaHash).HasColumnName("SENHA_HASH").HasMaxLength(500).IsRequired();
        builder.Property(u => u.Perfil).HasColumnName("PERFIL").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.CriadoEm).HasColumnName("CRIADO_EM").IsRequired();

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_LUDO_USUARIO_EMAIL");
    }
}
