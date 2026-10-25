using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Application.Services;

public interface IJogoService
{
    Task<OperationResult<PageResult<JogoResponse>>> ListarAsync(JogoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default);

    Task<OperationResult<JogoResponse>> ObterAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<JogoResponse>> CriarAsync(JogoRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<JogoResponse>> AtualizarAsync(int id, JogoRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
