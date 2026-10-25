using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;

namespace Ludoteca.Application.Services;

public interface IEmprestimoService
{
    Task<OperationResult<PageResult<EmprestimoResponse>>> ListarAsync(EmprestimoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default);

    Task<OperationResult<EmprestimoResponse>> ObterAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<EmprestimoResponse>> RegistrarAsync(EmprestimoRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<EmprestimoResponse>> DevolverAsync(int id, CancellationToken cancellationToken = default);
}
