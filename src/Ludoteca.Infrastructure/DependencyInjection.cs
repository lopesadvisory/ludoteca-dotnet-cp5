using Ludoteca.Application.Abstractions.External;
using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Abstractions.Security;
using Ludoteca.Infrastructure.External;
using Ludoteca.Infrastructure.Persistence;
using Ludoteca.Infrastructure.Repositories;
using Ludoteca.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ludoteca.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexao = configuration.GetConnectionString("OracleConnection")
            ?? throw new InvalidOperationException("A string de conexão 'OracleConnection' não foi configurada.");

        services.AddDbContext<LudotecaDbContext>(opcoes =>
            opcoes.UseOracle(conexao, OpcoesOracle.Configurar));

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IJogoRepository, JogoRepository>();
        services.AddScoped<IMembroRepository, MembroRepository>();
        services.AddScoped<IEmprestimoRepository, EmprestimoRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Secao));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ISenhaHasher, SenhaHasher>();

        AdicionarClienteViaCep(services, configuration);

        return services;
    }

    private static void AdicionarClienteViaCep(IServiceCollection services, IConfiguration configuration)
    {
        var secao = configuration.GetSection(ViaCepOptions.Secao);
        services.Configure<ViaCepOptions>(secao);
        services.AddMemoryCache();

        var viaCep = secao.Get<ViaCepOptions>() ?? new ViaCepOptions();

        services.AddHttpClient<ICepService, ViaCepService>(cliente => cliente.BaseAddress = new Uri(viaCep.UrlBase))
            .AddStandardResilienceHandler(resiliencia =>
            {
                resiliencia.Retry.MaxRetryAttempts = viaCep.TentativasDeRetry;
                resiliencia.Retry.Delay = TimeSpan.FromMilliseconds(viaCep.AtrasoEntreTentativasMilissegundos);
                resiliencia.AttemptTimeout.Timeout = TimeSpan.FromSeconds(viaCep.TimeoutPorTentativaSegundos);
                resiliencia.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(viaCep.TimeoutTotalSegundos);

                // O circuit breaker exige uma janela de amostragem de pelo menos o dobro do timeout por tentativa.
                resiliencia.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(viaCep.TimeoutPorTentativaSegundos * 3);
            });
    }
}
