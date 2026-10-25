using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;

namespace Ludoteca.Application.Abstractions.Repositories;

public sealed record MembroFiltro(string? Nome);

public interface IMembroRepository
{
    Task<OperationResult<Membro>> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<Membro>> ObterRastreadoPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PageResult<Membro>> ListarAsync(MembroFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Membro membro, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Membro membro, CancellationToken cancellationToken = default);

    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
