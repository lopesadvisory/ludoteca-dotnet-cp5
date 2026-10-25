using Ludoteca.Application.Abstractions.External;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Application.Services;

public class EnderecoService : IEnderecoService
{
    private readonly ICepService _cepService;

    public EnderecoService(ICepService cepService)
    {
        _cepService = cepService;
    }

    public Task<OperationResult<EnderecoResponse>> ConsultarPorCepAsync(string cep, CancellationToken cancellationToken = default)
    {
        var cepNormalizado = Cep.Normalizar(cep);
        if (cepNormalizado is null)
        {
            return Task.FromResult(OperationResult<EnderecoResponse>.Failure(
                Error.Validacao("Endereco.CepInvalido", "CEP inválido. Informe 8 dígitos.")));
        }

        return _cepService.ConsultarAsync(cepNormalizado, cancellationToken);
    }
}
