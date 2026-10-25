using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ludoteca.Tests.Integration.Support;

public static class ApiHelper
{
    public static string NovoSufixo() => Guid.NewGuid().ToString("N")[..10];

    public static object NovoJogo(string sufixo, int minJogadores = 2, int maxJogadores = 4)
    {
        return new
        {
            titulo = $"{LudotecaApiFixture.PrefixoTitulo}{sufixo}",
            editora = "Devir",
            categoria = "Estratégia",
            minJogadores,
            maxJogadores,
            duracaoMediaMinutos = 60
        };
    }

    public static object NovoMembro(string sufixo, string cep = CepServiceFalso.CepSe)
    {
        return new
        {
            nome = "Membro de Teste",
            email = $"{LudotecaApiFixture.PrefixoEmail}{sufixo}@teste.com",
            telefone = "11999990000",
            cep
        };
    }

    public static async Task<JsonElement> LerJsonAsync(this HttpResponseMessage resposta)
    {
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static JsonElement Dados(this JsonElement json) => json.GetProperty("data");

    public static string[] Rels(this JsonElement json)
    {
        return json.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString()!).ToArray();
    }

    public static async Task<int> CriarJogoAsync(this HttpClient administrador, string? sufixo = null)
    {
        var resposta = await administrador.PostAsJsonAsync("/api/jogos", NovoJogo(sufixo ?? NovoSufixo()));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.LerJsonAsync()).Dados().GetProperty("id").GetInt32();
    }

    public static async Task<int> CriarMembroAsync(this HttpClient cliente, string? sufixo = null)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/membros", NovoMembro(sufixo ?? NovoSufixo()));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.LerJsonAsync()).Dados().GetProperty("id").GetInt32();
    }

    public static async Task<int> CriarEmprestimoAsync(this HttpClient cliente, int jogoId, int membroId)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/emprestimos", new { jogoId, membroId, diasDeEmprestimo = 7 });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.LerJsonAsync()).Dados().GetProperty("id").GetInt32();
    }
}
