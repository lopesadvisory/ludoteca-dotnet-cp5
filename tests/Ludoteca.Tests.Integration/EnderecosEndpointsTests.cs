using System.Net;
using Ludoteca.Tests.Integration.Support;
using Xunit;

namespace Ludoteca.Tests.Integration;

[Collection(ApiCollection.Nome)]
public class EnderecosEndpointsTests
{
    private readonly LudotecaApiFixture _api;

    public EnderecosEndpointsTests(LudotecaApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task GetEndereco_CepValido_RetornaOkComEnderecoELinks()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.GetAsync("/api/enderecos/01001-000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Praça da Sé", json.Dados().GetProperty("logradouro").GetString());
        Assert.Equal("São Paulo", json.Dados().GetProperty("cidade").GetString());
        Assert.Contains("self", json.Rels());
    }

    [Fact]
    public async Task GetEndereco_CepInexistente_RetornaNotFound()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.GetAsync("/api/enderecos/00000000");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Endereco.CepNaoEncontrado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task GetEndereco_CepMalFormatado_RetornaBadRequest()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.GetAsync("/api/enderecos/123");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task GetEndereco_ServicoExternoForaDoAr_RetornaServiceUnavailable()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.GetAsync("/api/enderecos/99999999");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Endereco.ServicoIndisponivel", json.GetProperty("codigo").GetString());
    }
}
