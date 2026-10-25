using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ludoteca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ludoteca.Tests.Integration.Support;

public sealed class LudotecaApiFixture : IAsyncLifetime
{
    public const string PrefixoEmail = "teste.cp5.";
    public const string PrefixoTitulo = "Teste CP5 ";

    private string? _tokenAdministrador;
    private string? _tokenAtendente;

    public LudotecaApiFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        // Criar o cliente sobe a aplicação, que aplica as migrations e o seed do administrador.
        using var cliente = Factory.CreateClient();
        await LimparDadosDeTesteAsync();
    }

    public async Task DisposeAsync()
    {
        await LimparDadosDeTesteAsync();
        await Factory.DisposeAsync();
    }

    public HttpClient CriarClienteAnonimo() => Factory.CreateClient();

    public async Task<HttpClient> CriarClienteAdministradorAsync()
    {
        _tokenAdministrador ??= await ObterTokenDoAdministradorAsync();
        return ComToken(_tokenAdministrador);
    }

    public async Task<HttpClient> CriarClienteAtendenteAsync()
    {
        _tokenAtendente ??= await RegistrarEEntrarComoAtendenteAsync();
        return ComToken(_tokenAtendente);
    }

    public string EmailDoAdministrador => Configuracao["Administrador:Email"]!;

    public string SenhaDoAdministrador => Configuracao["Administrador:Senha"]!;

    private IConfiguration Configuracao => Factory.Services.GetRequiredService<IConfiguration>();

    private HttpClient ComToken(string token)
    {
        var cliente = Factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private async Task<string> ObterTokenDoAdministradorAsync()
    {
        using var cliente = Factory.CreateClient();
        return await EntrarAsync(cliente, EmailDoAdministrador, SenhaDoAdministrador);
    }

    private async Task<string> RegistrarEEntrarComoAtendenteAsync()
    {
        const string senha = "Senha@123";
        var email = $"{PrefixoEmail}atendente@ludoteca.test";

        using var cliente = Factory.CreateClient();
        var registro = await cliente.PostAsJsonAsync("/api/auth/registrar", new { nome = "Atendente de Teste", email, senha });
        registro.EnsureSuccessStatusCode();

        return await EntrarAsync(cliente, email, senha);
    }

    private static async Task<string> EntrarAsync(HttpClient cliente, string email, string senha)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha });
        resposta.EnsureSuccessStatusCode();

        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("token").GetString()!;
    }

    // Remove só o que os testes criaram, identificado pelos prefixos; o administrador do seed permanece.
    private async Task LimparDadosDeTesteAsync()
    {
        using var escopo = Factory.Services.CreateScope();
        var context = escopo.ServiceProvider.GetRequiredService<LudotecaDbContext>();

        var emprestimos = await context.Emprestimos
            .Where(e => e.Jogo.Titulo.StartsWith(PrefixoTitulo) || e.Membro.Email.StartsWith(PrefixoEmail))
            .ToListAsync();
        context.Emprestimos.RemoveRange(emprestimos);

        context.Jogos.RemoveRange(await context.Jogos.Where(j => j.Titulo.StartsWith(PrefixoTitulo)).ToListAsync());
        context.Membros.RemoveRange(await context.Membros.Where(m => m.Email.StartsWith(PrefixoEmail)).ToListAsync());
        context.Usuarios.RemoveRange(await context.Usuarios.Where(u => u.Email.StartsWith(PrefixoEmail)).ToListAsync());

        await context.SaveChangesAsync();
    }
}

[CollectionDefinition(Nome)]
public class ApiCollection : ICollectionFixture<LudotecaApiFixture>
{
    public const string Nome = "Api";
}
