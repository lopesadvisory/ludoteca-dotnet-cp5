using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;

namespace Ludoteca.Application.Abstractions.Repositories;

public interface IUsuarioRepository
{
    Task<OperationResult<Usuario>> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
