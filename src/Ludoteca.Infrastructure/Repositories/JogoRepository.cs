using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ludoteca.Infrastructure.Repositories;

public class JogoRepository : IJogoRepository
{
    private readonly LudotecaDbContext _context;

    public JogoRepository(LudotecaDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<Jogo>> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var jogo = await _context.Jogos.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        return jogo is null ? NaoEncontrado(id) : OperationResult<Jogo>.Success(jogo);
    }

    public async Task<OperationResult<Jogo>> ObterRastreadoPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var jogo = await _context.Jogos.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        return jogo is null ? NaoEncontrado(id) : OperationResult<Jogo>.Success(jogo);
    }

    public async Task<PageResult<Jogo>> ListarAsync(JogoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default)
    {
        var consulta = _context.Jogos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Titulo))
        {
            var titulo = filtro.Titulo.Trim().ToUpper();
            consulta = consulta.Where(j => j.Titulo.ToUpper().Contains(titulo));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            var categoria = filtro.Categoria.Trim().ToUpper();
            consulta = consulta.Where(j => j.Categoria.ToUpper() == categoria);
        }

        if (filtro.Disponivel is not null)
            consulta = consulta.Where(j => j.Disponivel == filtro.Disponivel);

        var total = await consulta.CountAsync(cancellationToken);

        var itens = await consulta
            .OrderBy(j => j.Titulo)
            .ThenBy(j => j.Id)
            .Skip(paginacao.Skip)
            .Take(paginacao.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<Jogo>(itens, paginacao.Page, paginacao.PageSize, total);
    }

    public async Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default)
    {
        var tituloEmUso = await _context.Jogos.AnyAsync(j => j.Titulo.ToUpper() == jogo.Titulo.ToUpper(), cancellationToken);
        if (tituloEmUso)
            throw TituloDuplicado(jogo.Titulo);

        _context.Jogos.Add(jogo);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (ErrosDeBanco.EhViolacaoDeUnicidade(excecao))
        {
            throw TituloDuplicado(jogo.Titulo);
        }
    }

    public async Task AtualizarAsync(Jogo jogo, CancellationToken cancellationToken = default)
    {
        var tituloEmUso = await _context.Jogos.AnyAsync(
            j => j.Id != jogo.Id && j.Titulo.ToUpper() == jogo.Titulo.ToUpper(),
            cancellationToken);

        if (tituloEmUso)
            throw TituloDuplicado(jogo.Titulo);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflitoDeDadosException(
                "Jogo.ConflitoDeConcorrencia",
                "O jogo foi alterado por outra operação. Consulte-o novamente e repita a alteração.");
        }
        catch (DbUpdateException excecao) when (ErrosDeBanco.EhViolacaoDeUnicidade(excecao))
        {
            throw TituloDuplicado(jogo.Titulo);
        }
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var jogo = await _context.Jogos.FirstOrDefaultAsync(j => j.Id == id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Jogo.NaoEncontrado", $"Jogo {id} não encontrado.");

        if (await _context.Emprestimos.AnyAsync(e => e.JogoId == id, cancellationToken))
            throw JogoComEmprestimos();

        _context.Jogos.Remove(jogo);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (ErrosDeBanco.EhViolacaoDeIntegridade(excecao))
        {
            throw JogoComEmprestimos();
        }
    }

    private static OperationResult<Jogo> NaoEncontrado(int id)
    {
        return OperationResult<Jogo>.Failure(Error.NaoEncontrado("Jogo.NaoEncontrado", $"Jogo {id} não encontrado."));
    }

    private static ConflitoDeDadosException TituloDuplicado(string titulo)
    {
        return new ConflitoDeDadosException("Jogo.TituloDuplicado", $"Já existe um jogo cadastrado com o título '{titulo}'.");
    }

    private static ConflitoDeDadosException JogoComEmprestimos()
    {
        return new ConflitoDeDadosException(
            "Jogo.PossuiEmprestimos",
            "Não é possível excluir um jogo que possui empréstimos registrados.");
    }
}
