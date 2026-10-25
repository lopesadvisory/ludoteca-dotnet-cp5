using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ludoteca.Infrastructure.Repositories;

public class MembroRepository : IMembroRepository
{
    private readonly LudotecaDbContext _context;

    public MembroRepository(LudotecaDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<Membro>> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var membro = await _context.Membros.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return membro is null ? NaoEncontrado(id) : OperationResult<Membro>.Success(membro);
    }

    public async Task<OperationResult<Membro>> ObterRastreadoPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var membro = await _context.Membros.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return membro is null ? NaoEncontrado(id) : OperationResult<Membro>.Success(membro);
    }

    public async Task<PageResult<Membro>> ListarAsync(MembroFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default)
    {
        var consulta = _context.Membros.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Nome))
        {
            var nome = filtro.Nome.Trim().ToUpper();
            consulta = consulta.Where(m => m.Nome.ToUpper().Contains(nome));
        }

        var total = await consulta.CountAsync(cancellationToken);

        var itens = await consulta
            .OrderBy(m => m.Nome)
            .ThenBy(m => m.Id)
            .Skip(paginacao.Skip)
            .Take(paginacao.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<Membro>(itens, paginacao.Page, paginacao.PageSize, total);
    }

    public async Task AdicionarAsync(Membro membro, CancellationToken cancellationToken = default)
    {
        if (await _context.Membros.AnyAsync(m => m.Email == membro.Email, cancellationToken))
            throw EmailDuplicado(membro.Email);

        _context.Membros.Add(membro);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (ErrosDeBanco.EhViolacaoDeUnicidade(excecao))
        {
            throw EmailDuplicado(membro.Email);
        }
    }

    public async Task AtualizarAsync(Membro membro, CancellationToken cancellationToken = default)
    {
        if (await _context.Membros.AnyAsync(m => m.Id != membro.Id && m.Email == membro.Email, cancellationToken))
            throw EmailDuplicado(membro.Email);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (ErrosDeBanco.EhViolacaoDeUnicidade(excecao))
        {
            throw EmailDuplicado(membro.Email);
        }
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var membro = await _context.Membros.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Membro.NaoEncontrado", $"Membro {id} não encontrado.");

        if (await _context.Emprestimos.AnyAsync(e => e.MembroId == id, cancellationToken))
            throw MembroComEmprestimos();

        _context.Membros.Remove(membro);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (ErrosDeBanco.EhViolacaoDeIntegridade(excecao))
        {
            throw MembroComEmprestimos();
        }
    }

    private static OperationResult<Membro> NaoEncontrado(int id)
    {
        return OperationResult<Membro>.Failure(Error.NaoEncontrado("Membro.NaoEncontrado", $"Membro {id} não encontrado."));
    }

    private static ConflitoDeDadosException EmailDuplicado(string email)
    {
        return new ConflitoDeDadosException("Membro.EmailDuplicado", $"Já existe um membro cadastrado com o e-mail '{email}'.");
    }

    private static ConflitoDeDadosException MembroComEmprestimos()
    {
        return new ConflitoDeDadosException(
            "Membro.PossuiEmprestimos",
            "Não é possível excluir um membro que possui empréstimos registrados.");
    }
}
