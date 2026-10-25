using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ludoteca.Infrastructure.Repositories;

public class EmprestimoRepository : IEmprestimoRepository
{
    private readonly LudotecaDbContext _context;

    public EmprestimoRepository(LudotecaDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<EmprestimoDetalhe>> ObterDetalheAsync(int id, CancellationToken cancellationToken = default)
    {
        var detalhe = await _context.Emprestimos
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EmprestimoDetalhe(
                e.Id,
                e.JogoId,
                e.Jogo.Titulo,
                e.MembroId,
                e.Membro.Nome,
                e.DataEmprestimo,
                e.DataPrevistaDevolucao,
                e.DataDevolucao))
            .FirstOrDefaultAsync(cancellationToken);

        return detalhe is null
            ? OperationResult<EmprestimoDetalhe>.Failure(NaoEncontrado(id))
            : OperationResult<EmprestimoDetalhe>.Success(detalhe);
    }

    public async Task<OperationResult<Emprestimo>> ObterRastreadoPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var emprestimo = await _context.Emprestimos
            .Include(e => e.Jogo)
            .Include(e => e.Membro)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return emprestimo is null
            ? OperationResult<Emprestimo>.Failure(NaoEncontrado(id))
            : OperationResult<Emprestimo>.Success(emprestimo);
    }

    public async Task<PageResult<EmprestimoDetalhe>> ListarAsync(EmprestimoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default)
    {
        var consulta = _context.Emprestimos.AsNoTracking();

        if (filtro.MembroId is not null)
            consulta = consulta.Where(e => e.MembroId == filtro.MembroId);

        if (filtro.JogoId is not null)
            consulta = consulta.Where(e => e.JogoId == filtro.JogoId);

        if (filtro.Ativos == true)
            consulta = consulta.Where(e => e.DataDevolucao == null);
        else if (filtro.Ativos == false)
            consulta = consulta.Where(e => e.DataDevolucao != null);

        var total = await consulta.CountAsync(cancellationToken);

        // A projeção traz só as colunas usadas na resposta, sem materializar Jogo e Membro inteiros.
        var itens = await consulta
            .OrderByDescending(e => e.DataEmprestimo)
            .ThenByDescending(e => e.Id)
            .Skip(paginacao.Skip)
            .Take(paginacao.PageSize)
            .Select(e => new EmprestimoDetalhe(
                e.Id,
                e.JogoId,
                e.Jogo.Titulo,
                e.MembroId,
                e.Membro.Nome,
                e.DataEmprestimo,
                e.DataPrevistaDevolucao,
                e.DataDevolucao))
            .ToListAsync(cancellationToken);

        return new PageResult<EmprestimoDetalhe>(itens, paginacao.Page, paginacao.PageSize, total);
    }

    public Task<int> ContarAtivosPorMembroAsync(int membroId, CancellationToken cancellationToken = default)
    {
        return _context.Emprestimos.CountAsync(e => e.MembroId == membroId && e.DataDevolucao == null, cancellationToken);
    }

    public async Task AdicionarAsync(Emprestimo emprestimo, CancellationToken cancellationToken = default)
    {
        _context.Emprestimos.Add(emprestimo);
        await SalvarAsync(cancellationToken);
    }

    public Task AtualizarAsync(Emprestimo emprestimo, CancellationToken cancellationToken = default)
    {
        return SalvarAsync(cancellationToken);
    }

    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoDeDadosException(
                "Emprestimo.ConflitoDeConcorrencia",
                "O jogo foi alterado por outra operação. Consulte-o novamente e repita o procedimento.");
        }
    }

    private static Error NaoEncontrado(int id)
    {
        return Error.NaoEncontrado("Emprestimo.NaoEncontrado", $"Empréstimo {id} não encontrado.");
    }
}
