using Ludoteca.Application.Abstractions.Security;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ludoteca.Infrastructure.Persistence;

public static class InicializadorDeBanco
{
    public static async Task InicializarBancoAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var escopo = services.CreateScope();
        var provedor = escopo.ServiceProvider;

        var configuracao = provedor.GetRequiredService<IConfiguration>();
        var context = provedor.GetRequiredService<LudotecaDbContext>();

        if (configuracao.GetValue("Banco:MigrarNaInicializacao", true))
            await context.Database.MigrateAsync(cancellationToken);

        await GarantirAdministradorAsync(context, provedor, configuracao, cancellationToken);
    }

    private static async Task GarantirAdministradorAsync(
        LudotecaDbContext context,
        IServiceProvider provedor,
        IConfiguration configuracao,
        CancellationToken cancellationToken)
    {
        var secao = configuracao.GetSection("Administrador");
        var nome = secao["Nome"];
        var email = secao["Email"];
        var senha = secao["Senha"];

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return;

        var emailNormalizado = email.Trim().ToLowerInvariant();
        if (await context.Usuarios.AnyAsync(u => u.Email == emailNormalizado, cancellationToken))
            return;

        var hasher = provedor.GetRequiredService<ISenhaHasher>();
        var relogio = provedor.GetRequiredService<TimeProvider>();

        var administrador = new Usuario(
            nome,
            email,
            hasher.GerarHash(senha),
            PerfilUsuario.Administrador,
            relogio.GetUtcNow().UtcDateTime);

        context.Usuarios.Add(administrador);
        await context.SaveChangesAsync(cancellationToken);
    }
}
