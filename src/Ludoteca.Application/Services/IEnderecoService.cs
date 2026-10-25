using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Application.Services;

public interface IEnderecoService
{
    Task<OperationResult<EnderecoResponse>> ConsultarPorCepAsync(string cep, CancellationToken cancellationToken = default);
}
