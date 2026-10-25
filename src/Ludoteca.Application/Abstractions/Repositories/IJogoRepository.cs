using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;

namespace Ludoteca.Application.Abstractions.Repositories;

public sealed record JogoFiltro(string? Titulo, string? Categoria, bool? Disponivel);

public interface IJogoRepository
{
    // Consultas devolvem OperationResult com o erro dentro do retorno.
    Task<OperationResult<Jogo>> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult<Jogo>> ObterRastreadoPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PageResult<Jogo>> ListarAsync(JogoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default);

    // Comandos lançam ConflitoDeDadosException ou EntidadeNaoEncontradaException.
    Task AdicionarAsync(Jogo jogo, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Jogo jogo, CancellationToken cancellationToken = default);

    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
