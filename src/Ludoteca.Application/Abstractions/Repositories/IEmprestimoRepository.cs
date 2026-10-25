using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;

namespace Ludoteca.Application.Abstractions.Repositories;

public sealed record EmprestimoFiltro(int? MembroId, int? JogoId, bool? Ativos);

public sealed record EmprestimoDetalhe(
    int Id,
    int JogoId,
    string JogoTitulo,
    int MembroId,
    string MembroNome,
    DateTime DataEmprestimo,
    DateTime DataPrevistaDevolucao,
    DateTime? DataDevolucao)
{
    public bool Ativo => DataDevolucao is null;

    public bool EstaAtrasado(DateTime agora) => Ativo && agora > DataPrevistaDevolucao;
}

public interface IEmprestimoRepository
{
    Task<OperationResult<EmprestimoDetalhe>> ObterDetalheAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<Emprestimo>> ObterRastreadoPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PageResult<EmprestimoDetalhe>> ListarAsync(EmprestimoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default);

    Task<int> ContarAtivosPorMembroAsync(int membroId, CancellationToken cancellationToken = default);

    // Salva o empréstimo e a mudança de disponibilidade do jogo na mesma transação.
    Task AdicionarAsync(Emprestimo emprestimo, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Emprestimo emprestimo, CancellationToken cancellationToken = default);
}
