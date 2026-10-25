using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ludoteca.Application.Abstractions.External;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace Ludoteca.Infrastructure.External;

public class ViaCepService : ICepService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ViaCepOptions _opcoes;

    public ViaCepService(HttpClient httpClient, IMemoryCache cache, IOptions<ViaCepOptions> opcoes)
    {
        _httpClient = httpClient;
        _cache = cache;
        _opcoes = opcoes.Value;
    }

    public async Task<OperationResult<EnderecoResponse>> ConsultarAsync(string cep, CancellationToken cancellationToken = default)
    {
        var chave = $"viacep:{cep}";
        if (_cache.TryGetValue(chave, out EnderecoResponse? emCache) && emCache is not null)
            return OperationResult<EnderecoResponse>.Success(emCache);

        try
        {
            using var resposta = await _httpClient.GetAsync($"ws/{cep}/json/", cancellationToken);

            if (resposta.StatusCode == HttpStatusCode.BadRequest)
                return OperationResult<EnderecoResponse>.Failure(Error.Validacao("Endereco.CepInvalido", "CEP inválido."));

            if (!resposta.IsSuccessStatusCode)
                return ServicoIndisponivel();

            var corpo = await resposta.Content.ReadFromJsonAsync<ViaCepResposta>(cancellationToken: cancellationToken);
            if (corpo is null)
                return ServicoIndisponivel();

            if (corpo.CepNaoExiste())
            {
                return OperationResult<EnderecoResponse>.Failure(
                    Error.NaoEncontrado("Endereco.CepNaoEncontrado", $"Nenhum endereço encontrado para o CEP {cep}."));
            }

            var endereco = new EnderecoResponse(cep, corpo.Logradouro, corpo.Bairro, corpo.Localidade ?? string.Empty, corpo.Uf ?? string.Empty);
            _cache.Set(chave, endereco, TimeSpan.FromHours(_opcoes.CacheEmHoras));

            return OperationResult<EnderecoResponse>.Success(endereco);
        }
        catch (Exception excecao) when (!cancellationToken.IsCancellationRequested
                                        && excecao is HttpRequestException
                                            or TimeoutException
                                            or JsonException
                                            or BrokenCircuitException
                                            or TaskCanceledException)
        {
            return ServicoIndisponivel();
        }
    }

    private static OperationResult<EnderecoResponse> ServicoIndisponivel()
    {
        return OperationResult<EnderecoResponse>.Failure(Error.ServicoExterno(
            "Endereco.ServicoIndisponivel",
            "O serviço de consulta de CEP está indisponível no momento. Tente novamente em instantes."));
    }

    private sealed class ViaCepResposta
    {
        public string? Logradouro { get; set; }

        public string? Bairro { get; set; }

        public string? Localidade { get; set; }

        public string? Uf { get; set; }

        // O ViaCEP responde {"erro": true} (ou "true", em versões antigas) quando o CEP tem formato válido mas não existe.
        public JsonElement Erro { get; set; }

        public bool CepNaoExiste()
        {
            return Erro.ValueKind == JsonValueKind.True
                   || (Erro.ValueKind == JsonValueKind.String && Erro.GetString() == "true");
        }
    }
}
