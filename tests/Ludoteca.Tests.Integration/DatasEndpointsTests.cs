using System.Net.Http.Json;
using Ludoteca.Tests.Integration.Support;
using Xunit;

namespace Ludoteca.Tests.Integration;

[Collection(ApiCollection.Nome)]
public class DatasEndpointsTests
{
    private readonly LudotecaApiFixture _api;

    public DatasEndpointsTests(LudotecaApiFixture api)
    {
        _api = api;
    }

    [Fact]
    public async Task GetJogoPorId_DataLidaDoBanco_VoltaEmUtcComOMesmoInstanteDoCadastro()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var resposta = await administrador.PostAsJsonAsync("/api/jogos", ApiHelper.NovoJogo(ApiHelper.NovoSufixo()));
        var cadastro = await resposta.LerJsonAsync();
        var id = cadastro.Dados().GetProperty("id").GetInt32();

        // Act
        var consulta = await (await administrador.GetAsync($"/api/jogos/{id}")).LerJsonAsync();

        // Assert
        var criadoEmNoCadastro = cadastro.Dados().GetProperty("criadoEm").GetString()!;
        var criadoEmNaConsulta = consulta.Dados().GetProperty("criadoEm").GetString()!;
        Assert.EndsWith("Z", criadoEmNoCadastro);
        Assert.EndsWith("Z", criadoEmNaConsulta);

        var diferenca = DateTimeOffset.Parse(criadoEmNaConsulta) - DateTimeOffset.Parse(criadoEmNoCadastro);
        Assert.True(Math.Abs(diferenca.TotalMilliseconds) < 1);
    }

    [Fact]
    public async Task GetEmprestimoPorId_DatasLidasDoBanco_VoltamEmUtc()
    {
        // Arrange
        var administrador = await _api.CriarClienteAdministradorAsync();
        var emprestimoId = await administrador.CriarEmprestimoAsync(
            await administrador.CriarJogoAsync(),
            await administrador.CriarMembroAsync());
        await administrador.PostAsync($"/api/emprestimos/{emprestimoId}/devolucao", null);

        // Act
        var dados = (await (await administrador.GetAsync($"/api/emprestimos/{emprestimoId}")).LerJsonAsync()).Dados();

        // Assert
        Assert.EndsWith("Z", dados.GetProperty("dataEmprestimo").GetString());
        Assert.EndsWith("Z", dados.GetProperty("dataPrevistaDevolucao").GetString());
        Assert.EndsWith("Z", dados.GetProperty("dataDevolucao").GetString());
    }
}
