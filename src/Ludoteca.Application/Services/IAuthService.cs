using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Application.Services;

public interface IAuthService
{
    Task<OperationResult<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<UsuarioResponse>> RegistrarAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default);
}
