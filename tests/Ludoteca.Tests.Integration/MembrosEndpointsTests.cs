using System.Net;
using System.Net.Http.Json;
using Ludoteca.Tests.Integration.Support;
using Xunit;

namespace Ludoteca.Tests.Integration;

[Collection(ApiCollection.Nome)]
public class MembrosEndpointsTests
{
    private readonly LudotecaApiFixture _api;

    public MembrosEndpointsTests(LudotecaApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task PostMembros_CepValido_RetornaCreatedComEnderecoPreenchidoELinks()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.PostAsJsonAsync("/api/membros", ApiHelper.NovoMembro(ApiHelper.NovoSufixo()));

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        var endereco = json.Dados().GetProperty("endereco");
        Assert.Equal("São Paulo", endereco.GetProperty("cidade").GetString());
        Assert.Equal("SP", endereco.GetProperty("uf").GetString());
        Assert.Equal(CepServiceFalso.CepSe, endereco.GetProperty("cep").GetString());
        Assert.Contains("emprestimos", json.Rels());
    }

    [Fact]
    public async Task PostMembros_CepInexistente_RetornaNotFound()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.PostAsJsonAsync(
            "/api/membros",
            ApiHelper.NovoMembro(ApiHelper.NovoSufixo(), cep: "00000-000"));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Endereco.CepNaoEncontrado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostMembros_ServicoDeCepForaDoAr_RetornaServiceUnavailable()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.PostAsJsonAsync(
            "/api/membros",
            ApiHelper.NovoMembro(ApiHelper.NovoSufixo(), cep: "99999-999"));

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Endereco.ServicoIndisponivel", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostMembros_CepMalFormatado_RetornaBadRequest()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.PostAsJsonAsync(
            "/api/membros",
            ApiHelper.NovoMembro(ApiHelper.NovoSufixo(), cep: "123"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostMembros_EmailJaCadastrado_RetornaConflict()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();
        var sufixo = ApiHelper.NovoSufixo();
        await atendente.CriarMembroAsync(sufixo);

        // Act
        var resposta = await atendente.PostAsJsonAsync("/api/membros", ApiHelper.NovoMembro(sufixo));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Membro.EmailDuplicado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task GetMembroPorId_Existente_RetornaOkComLinks()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();
        var id = await atendente.CriarMembroAsync();

        // Act
        var resposta = await atendente.GetAsync($"/api/membros/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var rels = (await resposta.LerJsonAsync()).Rels();
        Assert.Contains("self", rels);
        Assert.Contains("atualizar", rels);
        Assert.DoesNotContain("remover", rels);
    }

    [Fact]
    public async Task GetMembroPorId_Administrador_RecebeLinkDeRemover()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var id = await administrador.CriarMembroAsync();

        // Act
        var resposta = await administrador.GetAsync($"/api/membros/{id}");

        // Assert
        Assert.Contains("remover", (await resposta.LerJsonAsync()).Rels());
    }

    [Fact]
    public async Task GetMembroPorId_Inexistente_RetornaNotFound()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.GetAsync("/api/membros/999999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Membro.NaoEncontrado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task GetMembros_FiltroPorNomeEPaginacao_RetornaApenasOsMembrosDoFiltro()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();
        var marca = $"Filtro{ApiHelper.NovoSufixo()}";
        foreach (var indice in new[] { 1, 2, 3 })
        {
            await atendente.PostAsJsonAsync("/api/membros", new
            {
                nome = $"{marca} Pessoa {indice}",
                email = $"{LudotecaApiFixture.PrefixoEmail}{marca}{indice}@teste.com",
                cep = CepServiceFalso.CepSe
            });
        }

        // Act
        var json = await (await atendente.GetAsync($"/api/membros?nome={marca}&pageSize=2")).LerJsonAsync();

        // Assert
        var paginacao = json.GetProperty("pagination");
        Assert.Equal(3, paginacao.GetProperty("totalRecords").GetInt32());
        Assert.Equal(2, paginacao.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, json.GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task PutMembros_NovoCep_AtualizaDadosEEndereco()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();
        var sufixo = ApiHelper.NovoSufixo();
        var id = await atendente.CriarMembroAsync(sufixo);

        // Act
        var resposta = await atendente.PutAsJsonAsync($"/api/membros/{id}", new
        {
            nome = "Nome Alterado",
            email = $"{LudotecaApiFixture.PrefixoEmail}{sufixo}@teste.com",
            telefone = "21988887777",
            cep = "20040-020"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var dados = (await resposta.LerJsonAsync()).Dados();
        Assert.Equal("Nome Alterado", dados.GetProperty("nome").GetString());
        Assert.Equal("Rio de Janeiro", dados.GetProperty("endereco").GetProperty("cidade").GetString());
        Assert.Equal("RJ", dados.GetProperty("endereco").GetProperty("uf").GetString());
    }

    [Fact]
    public async Task PutMembros_Inexistente_RetornaNotFound()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.PutAsJsonAsync("/api/membros/999999999", ApiHelper.NovoMembro(ApiHelper.NovoSufixo()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task PutMembros_EmailDeOutroMembro_RetornaConflict()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();
        var sufixoExistente = ApiHelper.NovoSufixo();
        await atendente.CriarMembroAsync(sufixoExistente);
        var id = await atendente.CriarMembroAsync();

        // Act
        var resposta = await atendente.PutAsJsonAsync($"/api/membros/{id}", ApiHelper.NovoMembro(sufixoExistente));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task DeleteMembros_Atendente_RetornaForbidden()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();
        var id = await atendente.CriarMembroAsync();

        // Act
        var resposta = await atendente.DeleteAsync($"/api/membros/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task DeleteMembros_Administrador_RetornaNoContentEOMembroDeixaDeExistir()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var id = await administrador.CriarMembroAsync();

        // Act
        var exclusao = await administrador.DeleteAsync($"/api/membros/{id}");
        var consulta = await administrador.GetAsync($"/api/membros/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task DeleteMembros_ComEmprestimoRegistrado_RetornaConflict()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var membroId = await administrador.CriarMembroAsync();
        await administrador.CriarEmprestimoAsync(jogoId, membroId);

        // Act
        var resposta = await administrador.DeleteAsync($"/api/membros/{membroId}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Membro.PossuiEmprestimos", json.GetProperty("codigo").GetString());
    }
}
