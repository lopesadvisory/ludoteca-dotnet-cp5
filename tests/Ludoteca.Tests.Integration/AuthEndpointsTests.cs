using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ludoteca.Tests.Integration.Support;
using Xunit;

namespace Ludoteca.Tests.Integration;

[Collection(ApiCollection.Nome)]
public class AuthEndpointsTests
{
    private readonly LudotecaApiFixture _api;

    public AuthEndpointsTests(LudotecaApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task PostLogin_CredenciaisDoAdministrador_RetornaOkComTokenELinks()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email = _api.EmailDoAdministrador, senha = _api.SenhaDoAdministrador });

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.False(string.IsNullOrWhiteSpace(json.Dados().GetProperty("token").GetString()));
        Assert.Equal("Administrador", json.Dados().GetProperty("usuario").GetProperty("perfil").GetString());
        Assert.Contains("jogos", json.Rels());
        Assert.Contains("membros", json.Rels());
        Assert.Contains("emprestimos", json.Rels());
    }

    [Fact]
    public async Task PostLogin_SenhaIncorreta_RetornaUnauthorized()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email = _api.EmailDoAdministrador, senha = "senha-errada" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Auth.CredenciaisInvalidas", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostLogin_EmailNaoCadastrado_RetornaUnauthorized()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email = "ninguem@ludoteca.test", senha = "qualquer" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PostLogin_CorpoInvalido_RetornaBadRequest()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/auth/login", new { email = "nao-e-email", senha = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostRegistrar_EmailNovo_RetornaCreatedComPerfilAtendente()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();
        var email = $"{LudotecaApiFixture.PrefixoEmail}{ApiHelper.NovoSufixo()}@ludoteca.test";

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar", new { nome = "Novo Atendente", email, senha = "Senha@123" });

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Atendente", json.Dados().GetProperty("perfil").GetString());
        Assert.Equal(email, json.Dados().GetProperty("email").GetString());
    }

    [Fact]
    public async Task PostRegistrar_EmailJaCadastrado_RetornaConflict()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();
        var corpo = new
        {
            nome = "Atendente Repetido",
            email = $"{LudotecaApiFixture.PrefixoEmail}{ApiHelper.NovoSufixo()}@ludoteca.test",
            senha = "Senha@123"
        };
        await cliente.PostAsJsonAsync("/api/auth/registrar", corpo);

        // Act
        var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar", corpo);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Usuario.EmailDuplicado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostRegistrar_SenhaCurta_RetornaBadRequest()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();

        // Act
        var resposta = await cliente.PostAsJsonAsync(
            "/api/auth/registrar",
            new { nome = "Atendente", email = $"{LudotecaApiFixture.PrefixoEmail}curta@ludoteca.test", senha = "123" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Theory]
    [InlineData("/api/jogos")]
    [InlineData("/api/membros")]
    [InlineData("/api/emprestimos")]
    [InlineData("/api/enderecos/01001000")]
    public async Task Get_RotaProtegidaSemToken_RetornaUnauthorized(string rota)
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();

        // Act
        var resposta = await cliente.GetAsync(rota);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task GetJogos_TokenInvalido_RetornaUnauthorized()
    {
        // Arrange
        var cliente = _api.CriarClienteAnonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token.invalido.qualquer");

        // Act
        var resposta = await cliente.GetAsync("/api/jogos");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task GetJogos_ComTokenValido_RetornaOk()
    {
        // Arrange
        var cliente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await cliente.GetAsync("/api/jogos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
