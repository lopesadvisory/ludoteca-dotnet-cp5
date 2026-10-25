using Ludoteca.Application.Abstractions.External;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ludoteca.Tests.Integration.Support;

public sealed class LudotecaApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<ICepService>();
            servicos.AddSingleton<ICepService, CepServiceFalso>();
        });
    }
}
