using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Application.Abstractions.External;

public interface ICepService
{
    Task<OperationResult<EnderecoResponse>> ConsultarAsync(string cep, CancellationToken cancellationToken = default);
}
