using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ludoteca.Infrastructure.Persistence;

// Usada apenas pelas ferramentas do EF (dotnet ef), que precisam criar o contexto sem subir a API.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LudotecaDbContext>
{
    public LudotecaDbContext CreateDbContext(string[] args)
    {
        var configuracao = new ConfigurationBuilder()
            .SetBasePath(LocalizarPastaDaApi())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var opcoes = new DbContextOptionsBuilder<LudotecaDbContext>()
            .UseOracle(configuracao.GetConnectionString("OracleConnection"), OpcoesOracle.Configurar)
            .Options;

        return new LudotecaDbContext(opcoes);
    }

    private static string LocalizarPastaDaApi()
    {
        var diretorio = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (diretorio is not null)
        {
            var candidatos = new[]
            {
                Path.Combine(diretorio.FullName, "src", "Ludoteca.Api"),
                Path.Combine(diretorio.FullName, "Ludoteca.Api")
            };

            foreach (var candidato in candidatos)
            {
                if (File.Exists(Path.Combine(candidato, "appsettings.json")))
                    return candidato;
            }

            diretorio = diretorio.Parent;
        }

        throw new InvalidOperationException("Não foi possível localizar o appsettings.json do projeto Ludoteca.Api.");
    }
}
