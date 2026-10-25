using System.Net;
using System.Net.Http.Json;
using Ludoteca.Tests.Integration.Support;
using Xunit;

namespace Ludoteca.Tests.Integration;

[Collection(ApiCollection.Nome)]
public class JogosEndpointsTests
{
    private readonly LudotecaApiFixture _api;

    public JogosEndpointsTests(LudotecaApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task PostJogos_AdministradorComDadosValidos_RetornaCreatedComLocationELinks()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/jogos", ApiHelper.NovoJogo(ApiHelper.NovoSufixo()));

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        var json = await resposta.LerJsonAsync();
        Assert.True(json.Dados().GetProperty("disponivel").GetBoolean());
        Assert.Contains("self", json.Rels());
    }

    [Fact]
    public async Task PostJogos_Atendente_RetornaForbidden()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();

        // Act
        var resposta = await atendente.PostAsJsonAsync("/api/jogos", ApiHelper.NovoJogo(ApiHelper.NovoSufixo()));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task PostJogos_TituloDuplicado_RetornaConflict()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var sufixo = ApiHelper.NovoSufixo();
        await administrador.CriarJogoAsync(sufixo);

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/jogos", ApiHelper.NovoJogo(sufixo));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Jogo.TituloDuplicado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostJogos_MaximoMenorQueMinimo_RetornaUnprocessableEntity()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.PostAsJsonAsync(
            "/api/jogos",
            ApiHelper.NovoJogo(ApiHelper.NovoSufixo(), minJogadores: 5, maxJogadores: 2));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Jogo.MaxJogadoresInvalido", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostJogos_CorpoInvalido_RetornaBadRequestComErrosDeValidacao()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/jogos", new { titulo = "" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.True(json.TryGetProperty("errors", out var erros));
        Assert.True(erros.TryGetProperty("Titulo", out _));
    }

    [Fact]
    public async Task GetJogoPorId_Existente_RetornaOkComLinksDeAdministrador()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var id = await administrador.CriarJogoAsync();

        // Act
        var resposta = await administrador.GetAsync($"/api/jogos/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var rels = (await resposta.LerJsonAsync()).Rels();
        Assert.Contains("self", rels);
        Assert.Contains("colecao", rels);
        Assert.Contains("emprestar", rels);
        Assert.Contains("atualizar", rels);
        Assert.Contains("remover", rels);
    }

    [Fact]
    public async Task GetJogoPorId_Atendente_NaoRecebeLinksDeAtualizarERemover()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var atendente = await _api.CriarClienteAtendenteAsync();
        var id = await administrador.CriarJogoAsync();

        // Act
        var resposta = await atendente.GetAsync($"/api/jogos/{id}");

        // Assert
        var rels = (await resposta.LerJsonAsync()).Rels();
        Assert.Contains("emprestar", rels);
        Assert.DoesNotContain("atualizar", rels);
        Assert.DoesNotContain("remover", rels);
    }

    [Fact]
    public async Task GetJogoPorId_JogoEmprestado_NaoRecebeLinkDeEmprestar()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var membroId = await administrador.CriarMembroAsync();
        await administrador.CriarEmprestimoAsync(jogoId, membroId);

        // Act
        var resposta = await administrador.GetAsync($"/api/jogos/{jogoId}");

        // Assert
        var json = await resposta.LerJsonAsync();
        Assert.False(json.Dados().GetProperty("disponivel").GetBoolean());
        Assert.DoesNotContain("emprestar", json.Rels());
    }

    [Fact]
    public async Task GetJogoPorId_Inexistente_RetornaNotFoundComProblemDetails()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.GetAsync("/api/jogos/999999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Jogo.NaoEncontrado", json.GetProperty("codigo").GetString());
        Assert.Equal(404, json.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetJogos_ComPaginacao_RetornaMetadadosPageResultELinksDeNavegacao()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var sufixo = $"Paginacao {ApiHelper.NovoSufixo()}";
        foreach (var letra in new[] { "A", "B", "C" })
            await administrador.CriarJogoAsync($"{sufixo} {letra}");

        var filtro = Uri.EscapeDataString($"{LudotecaApiFixture.PrefixoTitulo}{sufixo}");

        // Act
        var primeira = await (await administrador.GetAsync($"/api/jogos?titulo={filtro}&pageSize=2&page=1")).LerJsonAsync();
        var segunda = await (await administrador.GetAsync($"/api/jogos?titulo={filtro}&pageSize=2&page=2")).LerJsonAsync();

        // Assert
        var paginacao = primeira.GetProperty("pagination");
        Assert.Equal(3, paginacao.GetProperty("totalRecords").GetInt32());
        Assert.Equal(2, paginacao.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, paginacao.GetProperty("itemCount").GetInt32());
        Assert.True(paginacao.GetProperty("hasNext").GetBoolean());
        Assert.False(paginacao.GetProperty("hasPrevious").GetBoolean());
        Assert.Equal(2, primeira.GetProperty("data").GetArrayLength());
        Assert.Contains("next", primeira.Rels());
        Assert.DoesNotContain("prev", primeira.Rels());

        Assert.Equal(1, segunda.GetProperty("pagination").GetProperty("itemCount").GetInt32());
        Assert.True(segunda.GetProperty("pagination").GetProperty("hasPrevious").GetBoolean());
        Assert.Contains("prev", segunda.Rels());
        Assert.DoesNotContain("next", segunda.Rels());
    }

    [Fact]
    public async Task GetJogos_CadaItemDaColecao_TrazSeusProprioLinks()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var sufixo = $"Colecao {ApiHelper.NovoSufixo()}";
        await administrador.CriarJogoAsync(sufixo);
        var filtro = Uri.EscapeDataString($"{LudotecaApiFixture.PrefixoTitulo}{sufixo}");

        // Act
        var json = await (await administrador.GetAsync($"/api/jogos?titulo={filtro}")).LerJsonAsync();

        // Assert
        var item = json.GetProperty("data")[0];
        Assert.Contains("self", item.Rels());
        Assert.Contains("emprestar", item.Rels());
    }

    [Fact]
    public async Task GetJogos_FiltroPorDisponibilidade_RetornaApenasJogosDisponiveis()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var sufixo = $"Filtro {ApiHelper.NovoSufixo()}";
        var livre = await administrador.CriarJogoAsync($"{sufixo} livre");
        var emprestado = await administrador.CriarJogoAsync($"{sufixo} emprestado");
        var membroId = await administrador.CriarMembroAsync();
        await administrador.CriarEmprestimoAsync(emprestado, membroId);
        var titulo = Uri.EscapeDataString($"{LudotecaApiFixture.PrefixoTitulo}{sufixo}");

        // Act
        var json = await (await administrador.GetAsync($"/api/jogos?titulo={titulo}&disponivel=true")).LerJsonAsync();

        // Assert
        var ids = json.GetProperty("data").EnumerateArray().Select(i => i.Dados().GetProperty("id").GetInt32()).ToArray();
        Assert.Contains(livre, ids);
        Assert.DoesNotContain(emprestado, ids);
    }

    [Fact]
    public async Task PutJogos_AdministradorComDadosValidos_AtualizaOJogo()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var id = await administrador.CriarJogoAsync();
        var novoSufixo = ApiHelper.NovoSufixo();

        // Act
        var resposta = await administrador.PutAsJsonAsync($"/api/jogos/{id}", ApiHelper.NovoJogo(novoSufixo, minJogadores: 1, maxJogadores: 6));

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var dados = (await resposta.LerJsonAsync()).Dados();
        Assert.Equal($"{LudotecaApiFixture.PrefixoTitulo}{novoSufixo}", dados.GetProperty("titulo").GetString());
        Assert.Equal(6, dados.GetProperty("maxJogadores").GetInt32());
    }

    [Fact]
    public async Task PutJogos_TituloDeOutroJogo_RetornaConflict()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var sufixoExistente = ApiHelper.NovoSufixo();
        await administrador.CriarJogoAsync(sufixoExistente);
        var id = await administrador.CriarJogoAsync();

        // Act
        var resposta = await administrador.PutAsJsonAsync($"/api/jogos/{id}", ApiHelper.NovoJogo(sufixoExistente));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task PutJogos_Inexistente_RetornaNotFound()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.PutAsJsonAsync("/api/jogos/999999999", ApiHelper.NovoJogo(ApiHelper.NovoSufixo()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task PutJogos_Atendente_RetornaForbidden()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var atendente = await _api.CriarClienteAtendenteAsync();
        var id = await administrador.CriarJogoAsync();

        // Act
        var resposta = await atendente.PutAsJsonAsync($"/api/jogos/{id}", ApiHelper.NovoJogo(ApiHelper.NovoSufixo()));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task DeleteJogos_Administrador_RetornaNoContentEOJogoDeixaDeExistir()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var id = await administrador.CriarJogoAsync();

        // Act
        var exclusao = await administrador.DeleteAsync($"/api/jogos/{id}");
        var consulta = await administrador.GetAsync($"/api/jogos/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task DeleteJogos_Atendente_RetornaForbiddenEMantemOJogo()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var atendente = await _api.CriarClienteAtendenteAsync();
        var id = await administrador.CriarJogoAsync();

        // Act
        var exclusao = await atendente.DeleteAsync($"/api/jogos/{id}");
        var consulta = await administrador.GetAsync($"/api/jogos/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, exclusao.StatusCode);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
    }

    [Fact]
    public async Task DeleteJogos_Inexistente_RetornaNotFound()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.DeleteAsync("/api/jogos/999999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task DeleteJogos_ComEmprestimoRegistrado_RetornaConflict()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var membroId = await administrador.CriarMembroAsync();
        await administrador.CriarEmprestimoAsync(jogoId, membroId);

        // Act
        var resposta = await administrador.DeleteAsync($"/api/jogos/{jogoId}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Jogo.PossuiEmprestimos", json.GetProperty("codigo").GetString());
    }
}
