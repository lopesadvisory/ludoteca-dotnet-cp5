using Ludoteca.Application.Abstractions.External;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Tests.Integration.Support;

// Substitui apenas a chamada de rede ao ViaCEP; o banco de dados usado nos testes é o mesmo da aplicação.
public sealed class CepServiceFalso : ICepService
{
    public const string CepSe = "01001000";
    public const string CepRio = "20040020";
    public const string CepInexistente = "00000000";
    public const string CepComServicoForaDoAr = "99999999";

    public Task<OperationResult<EnderecoResponse>> ConsultarAsync(string cep, CancellationToken cancellationToken = default)
    {
        var resultado = cep switch
        {
            CepSe => Sucesso(cep, "Praça da Sé", "Sé", "São Paulo", "SP"),
            CepRio => Sucesso(cep, "Rua da Assembleia", "Centro", "Rio de Janeiro", "RJ"),
            CepInexistente => OperationResult<EnderecoResponse>.Failure(
                Error.NaoEncontrado("Endereco.CepNaoEncontrado", $"Nenhum endereço encontrado para o CEP {cep}.")),
            CepComServicoForaDoAr => OperationResult<EnderecoResponse>.Failure(
                Error.ServicoExterno("Endereco.ServicoIndisponivel", "O serviço de consulta de CEP está indisponível no momento.")),
            _ => Sucesso(cep, "Rua de Teste", "Centro", "Campinas", "SP")
        };

        return Task.FromResult(resultado);
    }

    private static OperationResult<EnderecoResponse> Sucesso(string cep, string logradouro, string bairro, string cidade, string uf)
    {
        return OperationResult<EnderecoResponse>.Success(new EnderecoResponse(cep, logradouro, bairro, cidade, uf));
    }
}
