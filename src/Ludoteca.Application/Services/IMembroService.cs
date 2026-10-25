using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Application.Services;

public interface IMembroService
{
    Task<OperationResult<PageResult<MembroResponse>>> ListarAsync(MembroFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default);

    Task<OperationResult<MembroResponse>> ObterAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<MembroResponse>> CriarAsync(MembroRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<MembroResponse>> AtualizarAsync(int id, MembroRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
