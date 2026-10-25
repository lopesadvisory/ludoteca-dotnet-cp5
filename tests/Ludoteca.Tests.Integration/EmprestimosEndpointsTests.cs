using System.Net;
using System.Net.Http.Json;
using Ludoteca.Tests.Integration.Support;
using Xunit;

namespace Ludoteca.Tests.Integration;

[Collection(ApiCollection.Nome)]
public class EmprestimosEndpointsTests
{
    private readonly LudotecaApiFixture _api;

    public EmprestimosEndpointsTests(LudotecaApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task PostEmprestimos_JogoDisponivel_RetornaCreatedComStatusAtivoELinkDeDevolucao()
    {
        // Arrange
        var atendente = await _api.CriarClienteAtendenteAsync();
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var membroId = await atendente.CriarMembroAsync();

        // Act
        var resposta = await atendente.PostAsJsonAsync("/api/emprestimos", new { jogoId, membroId, diasDeEmprestimo = 7 });

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Ativo", json.Dados().GetProperty("status").GetString());
        Assert.Equal(jogoId, json.Dados().GetProperty("jogoId").GetInt32());
        Assert.Contains("devolver", json.Rels());
        Assert.Contains("jogo", json.Rels());
        Assert.Contains("membro", json.Rels());
    }

    [Fact]
    public async Task PostEmprestimos_JogoDisponivel_MarcaOJogoComoIndisponivel()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var membroId = await administrador.CriarMembroAsync();

        // Act
        await administrador.CriarEmprestimoAsync(jogoId, membroId);
        var jogo = await (await administrador.GetAsync($"/api/jogos/{jogoId}")).LerJsonAsync();

        // Assert
        Assert.False(jogo.Dados().GetProperty("disponivel").GetBoolean());
    }

    [Fact]
    public async Task PostEmprestimos_JogoJaEmprestado_RetornaUnprocessableEntity()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var primeiroMembro = await administrador.CriarMembroAsync();
        var segundoMembro = await administrador.CriarMembroAsync();
        await administrador.CriarEmprestimoAsync(jogoId, primeiroMembro);

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/emprestimos", new { jogoId, membroId = segundoMembro });

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Jogo.Indisponivel", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostEmprestimos_MembroNoLimiteDeTresAtivos_RetornaUnprocessableEntity()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var membroId = await administrador.CriarMembroAsync();
        for (var indice = 0; indice < 3; indice++)
            await administrador.CriarEmprestimoAsync(await administrador.CriarJogoAsync(), membroId);

        var quartoJogo = await administrador.CriarJogoAsync();

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/emprestimos", new { jogoId = quartoJogo, membroId });

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Emprestimo.LimiteExcedido", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostEmprestimos_JogoInexistente_RetornaNotFound()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var membroId = await administrador.CriarMembroAsync();

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/emprestimos", new { jogoId = 999999999, membroId });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Jogo.NaoEncontrado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostEmprestimos_MembroInexistente_RetornaNotFound()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/emprestimos", new { jogoId, membroId = 999999999 });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Membro.NaoEncontrado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostEmprestimos_CorpoInvalido_RetornaBadRequest()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/emprestimos", new { jogoId = 0, membroId = 0, diasDeEmprestimo = 99 });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostEmprestimos_SemToken_RetornaUnauthorized()
    {
        // Arrange
        var anonimo = _api.CriarClienteAnonimo();

        // Act
        var resposta = await anonimo.PostAsJsonAsync("/api/emprestimos", new { jogoId = 1, membroId = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PostDevolucao_EmprestimoAtivo_RetornaOkEJogoVoltaAFicarDisponivel()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var membroId = await administrador.CriarMembroAsync();
        var emprestimoId = await administrador.CriarEmprestimoAsync(jogoId, membroId);

        // Act
        var resposta = await administrador.PostAsync($"/api/emprestimos/{emprestimoId}/devolucao", null);
        var jogo = await (await administrador.GetAsync($"/api/jogos/{jogoId}")).LerJsonAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Devolvido", json.Dados().GetProperty("status").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, json.Dados().GetProperty("dataDevolucao").ValueKind);
        Assert.DoesNotContain("devolver", json.Rels());
        Assert.True(jogo.Dados().GetProperty("disponivel").GetBoolean());
    }

    [Fact]
    public async Task PostDevolucao_EmprestimoJaDevolvido_RetornaUnprocessableEntity()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var emprestimoId = await administrador.CriarEmprestimoAsync(
            await administrador.CriarJogoAsync(),
            await administrador.CriarMembroAsync());
        await administrador.PostAsync($"/api/emprestimos/{emprestimoId}/devolucao", null);

        // Act
        var resposta = await administrador.PostAsync($"/api/emprestimos/{emprestimoId}/devolucao", null);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Emprestimo.JaDevolvido", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task PostDevolucao_EmprestimoInexistente_RetornaNotFound()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.PostAsync("/api/emprestimos/999999999/devolucao", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task PostEmprestimos_AposDevolucao_JogoPodeSerEmprestadoNovamente()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var jogoId = await administrador.CriarJogoAsync();
        var membroId = await administrador.CriarMembroAsync();
        var emprestimoId = await administrador.CriarEmprestimoAsync(jogoId, membroId);
        await administrador.PostAsync($"/api/emprestimos/{emprestimoId}/devolucao", null);

        // Act
        var resposta = await administrador.PostAsJsonAsync("/api/emprestimos", new { jogoId, membroId });

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task GetEmprestimoPorId_Existente_RetornaOkComTituloDoJogoENomeDoMembro()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var sufixo = ApiHelper.NovoSufixo();
        var jogoId = await administrador.CriarJogoAsync(sufixo);
        var emprestimoId = await administrador.CriarEmprestimoAsync(jogoId, await administrador.CriarMembroAsync());

        // Act
        var resposta = await administrador.GetAsync($"/api/emprestimos/{emprestimoId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var dados = (await resposta.LerJsonAsync()).Dados();
        Assert.Equal($"{LudotecaApiFixture.PrefixoTitulo}{sufixo}", dados.GetProperty("jogoTitulo").GetString());
        Assert.Equal("Membro de Teste", dados.GetProperty("membroNome").GetString());
    }

    [Fact]
    public async Task GetEmprestimoPorId_Inexistente_RetornaNotFound()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();

        // Act
        var resposta = await administrador.GetAsync("/api/emprestimos/999999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var json = await resposta.LerJsonAsync();
        Assert.Equal("Emprestimo.NaoEncontrado", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task GetEmprestimos_FiltroPorMembroEAtivos_SeparaAtivosDeDevolvidos()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var membroId = await administrador.CriarMembroAsync();
        var ativoId = await administrador.CriarEmprestimoAsync(await administrador.CriarJogoAsync(), membroId);
        var devolvidoId = await administrador.CriarEmprestimoAsync(await administrador.CriarJogoAsync(), membroId);
        await administrador.PostAsync($"/api/emprestimos/{devolvidoId}/devolucao", null);

        // Act
        var ativos = await (await administrador.GetAsync($"/api/emprestimos?membroId={membroId}&ativos=true")).LerJsonAsync();
        var devolvidos = await (await administrador.GetAsync($"/api/emprestimos?membroId={membroId}&ativos=false")).LerJsonAsync();

        // Assert
        Assert.Equal(new[] { ativoId }, IdsDaColecao(ativos));
        Assert.Equal(new[] { devolvidoId }, IdsDaColecao(devolvidos));
    }

    [Fact]
    public async Task GetEmprestimos_ComPaginacao_RetornaMetadadosELinks()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var membroId = await administrador.CriarMembroAsync();
        for (var indice = 0; indice < 3; indice++)
        {
            var emprestimoId = await administrador.CriarEmprestimoAsync(await administrador.CriarJogoAsync(), membroId);
            await administrador.PostAsync($"/api/emprestimos/{emprestimoId}/devolucao", null);
        }

        // Act
        var json = await (await administrador.GetAsync($"/api/emprestimos?membroId={membroId}&pageSize=2&page=1")).LerJsonAsync();

        // Assert
        var paginacao = json.GetProperty("pagination");
        Assert.Equal(3, paginacao.GetProperty("totalRecords").GetInt32());
        Assert.Equal(2, paginacao.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, json.GetProperty("data").GetArrayLength());
        Assert.Contains("next", json.Rels());
    }

    private static int[] IdsDaColecao(System.Text.Json.JsonElement json)
    {
        return json.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.Dados().GetProperty("id").GetInt32())
            .ToArray();
    }
}
