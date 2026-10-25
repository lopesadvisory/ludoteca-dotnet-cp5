using System.Net;
using System.Text;
using Ludoteca.Application;
using Ludoteca.Application.Abstractions.External;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;

namespace Ludoteca.Tests.Unit.Infrastructure;

// Usa o mesmo registro da aplicação (AddInfrastructure), trocando apenas o handler que faria a chamada de rede.
public class ResilienciaViaCepTests
{
    private const string RespostaDaSe =
        "{\"cep\":\"01001-000\",\"logradouro\":\"Praça da Sé\",\"bairro\":\"Sé\",\"localidade\":\"São Paulo\",\"uf\":\"SP\"}";

    private sealed class ManipuladorEmSequencia : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> _respostas;

        public ManipuladorEmSequencia(params HttpStatusCode[] respostas)
        {
            _respostas = new Queue<HttpStatusCode>(respostas);
        }

        public int Chamadas { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++;

            // Depois que a sequência acaba, repete a última resposta.
            var status = _respostas.Count > 1 ? _respostas.Dequeue() : _respostas.Peek();

            var resposta = new HttpResponseMessage(status);
            if (status == HttpStatusCode.OK)
                resposta.Content = new StringContent(RespostaDaSe, Encoding.UTF8, "application/json");

            return Task.FromResult(resposta);
        }
    }

    private static ICepService CriarServico(ManipuladorEmSequencia manipulador)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OracleConnection"] = "User Id=teste;Password=teste;Data Source=localhost:1521/XE",
                ["ViaCep:UrlBase"] = "https://viacep.test/",
                ["ViaCep:TentativasDeRetry"] = "3",
                ["ViaCep:AtrasoEntreTentativasMilissegundos"] = "5"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuracao);
        services.AddApplication();
        services.AddInfrastructure(configuracao);
        services.Configure<HttpClientFactoryOptions>(
            nameof(ICepService),
            opcoes => opcoes.HttpMessageHandlerBuilderActions.Add(construtor => construtor.PrimaryHandler = manipulador));

        return services.BuildServiceProvider().GetRequiredService<ICepService>();
    }

    [Fact]
    public async Task ConsultarAsync_FalhaTemporariaSeguidaDeSucesso_TentaDeNovoERetornaOEndereco()
    {
        // Arrange
        var manipulador = new ManipuladorEmSequencia(HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway, HttpStatusCode.OK);
        var servico = CriarServico(manipulador);

        // Act
        var resultado = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("São Paulo", resultado.Data!.Cidade);
        Assert.Equal(3, manipulador.Chamadas);
    }

    [Fact]
    public async Task ConsultarAsync_ServicoSempreIndisponivel_EsgotaAsTentativasERetornaFalhaDeServicoExterno()
    {
        // Arrange
        var manipulador = new ManipuladorEmSequencia(HttpStatusCode.ServiceUnavailable);
        var servico = CriarServico(manipulador);

        // Act
        var resultado = await servico.ConsultarAsync("01001000");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.ServicoExterno, resultado.Error!.Tipo);
        Assert.Equal(4, manipulador.Chamadas);
    }

    [Fact]
    public async Task ConsultarAsync_RespostaDeCepInvalido_NaoRepeteAChamada()
    {
        // Arrange
        var manipulador = new ManipuladorEmSequencia(HttpStatusCode.BadRequest);
        var servico = CriarServico(manipulador);

        // Act
        var resultado = await servico.ConsultarAsync("12345678");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Validacao, resultado.Error!.Tipo);
        Assert.Equal(1, manipulador.Chamadas);
    }
}
