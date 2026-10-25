using Ludoteca.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ludoteca.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJogoService, JogoService>();
        services.AddScoped<IMembroService, MembroService>();
        services.AddScoped<IEmprestimoService, EmprestimoService>();
        services.AddScoped<IEnderecoService, EnderecoService>();

        return services;
    }
}
