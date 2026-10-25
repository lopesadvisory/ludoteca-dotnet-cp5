using System.Net;
using System.Text;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Infrastructure.External;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ludoteca.Tests.Unit.Infrastructure;

public class ViaCepServiceTests
{
    private sealed class ManipuladorFalso : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _resposta;

        public ManipuladorFalso(Func<HttpRequestMessage, HttpResponseMessage> resposta)
        {
            _resposta = resposta;
        }

        public int Chamadas { get; private set; }

        public string? UltimaUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++;
            UltimaUrl = request.RequestUri?.ToString();
            return Task.FromResult(_resposta(request));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string corpo)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(corpo, Encoding.UTF8, "application/json")
        };
    }

    private static ViaCepService CriarServico(ManipuladorFalso manipulador)
    {
        var cliente = new HttpClient(manipulador) { BaseAddress = new Uri("https://viacep.test/") };
        return new ViaCepService(cliente, new MemoryCache(new MemoryCacheOptions()), Options.Create(new ViaCepOptions()));
    }

    private const string RespostaDaSe =
        "{\"cep\":\"01001-000\",\"logradouro\":\"Praça da Sé\",\"bairro\":\"Sé\",\"localidade\":\"São Paulo\",\"uf\":\"SP\"}";

    [Fact]
    public async Task ConsultarAsync_CepExistente_RetornaOEndereco()
    {
        // Arrange
        var manipulador = new ManipuladorFalso(_ => Json(HttpStatusCode.OK, RespostaDaSe));
        var servico = CriarServico(manipulador);

        // Act
        var resultado = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("01001000", resultado.Data!.Cep);
        Assert.Equal("Praça da Sé", resultado.Data.Logradouro);
        Assert.Equal("São Paulo", resultado.Data.Cidade);
        Assert.Equal("SP", resultado.Data.Uf);
        Assert.Equal("https://viacep.test/ws/01001000/json/", manipulador.UltimaUrl);
    }

    [Fact]
    public async Task ConsultarAsync_MesmoCepDuasVezes_UsaOCacheNaSegundaConsulta()
    {
        // Arrange
        var manipulador = new ManipuladorFalso(_ => Json(HttpStatusCode.OK, RespostaDaSe));
        var servico = CriarServico(manipulador);

        // Act
        await servico.ConsultarAsync("01001000");
        var segunda = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(segunda.IsSuccess);
        Assert.Equal(1, manipulador.Chamadas);
    }

    [Theory]
    [InlineData("{\"erro\": true}")]
    [InlineData("{\"erro\": \"true\"}")]
    public async Task ConsultarAsync_CepInexistente_RetornaFalhaNaoEncontrado(string corpo)
    {
        // Arrange
        var servico = CriarServico(new ManipuladorFalso(_ => Json(HttpStatusCode.OK, corpo)));

        // Act
        var resultado = await servico.ConsultarAsync("00000000");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Error!.Tipo);
        Assert.Equal("Endereco.CepNaoEncontrado", resultado.Error.Codigo);
    }

    [Fact]
    public async Task ConsultarAsync_ServicoRespondeBadRequest_RetornaFalhaDeValidacao()
    {
        // Arrange
        var servico = CriarServico(new ManipuladorFalso(_ => Json(HttpStatusCode.BadRequest, "")));

        // Act
        var resultado = await servico.ConsultarAsync("12345678");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Validacao, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ConsultarAsync_ServicoRespondeErroDeServidor_RetornaFalhaDeServicoExterno()
    {
        // Arrange
        var servico = CriarServico(new ManipuladorFalso(_ => Json(HttpStatusCode.InternalServerError, "")));

        // Act
        var resultado = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.ServicoExterno, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ConsultarAsync_FalhaDeRede_RetornaFalhaDeServicoExterno()
    {
        // Arrange
        var servico = CriarServico(new ManipuladorFalso(_ => throw new HttpRequestException("sem conexão")));

        // Act
        var resultado = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.ServicoExterno, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ConsultarAsync_TempoEsgotado_RetornaFalhaDeServicoExterno()
    {
        // Arrange
        var servico = CriarServico(new ManipuladorFalso(_ => throw new TaskCanceledException("timeout")));

        // Act
        var resultado = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Endereco.ServicoIndisponivel", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ConsultarAsync_CorpoInvalido_RetornaFalhaDeServicoExterno()
    {
        // Arrange
        var servico = CriarServico(new ManipuladorFalso(_ => Json(HttpStatusCode.OK, "isto não é json")));

        // Act
        var resultado = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.ServicoExterno, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ConsultarAsync_FalhaNaoEhGuardadaNoCache_ProximaConsultaChamaOServicoDeNovo()
    {
        // Arrange
        var manipulador = new ManipuladorFalso(_ => Json(HttpStatusCode.InternalServerError, ""));
        var servico = CriarServico(manipulador);

        // Act
        await servico.ConsultarAsync("01001000");
        await servico.ConsultarAsync("01001000");

        // Assert
        Assert.Equal(2, manipulador.Chamadas);
    }
}
