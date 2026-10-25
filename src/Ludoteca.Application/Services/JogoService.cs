using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Application.Services;

public class JogoService : IJogoService
{
    private readonly IJogoRepository _jogos;
    private readonly TimeProvider _relogio;

    public JogoService(IJogoRepository jogos, TimeProvider relogio)
    {
        _jogos = jogos;
        _relogio = relogio;
    }

    public async Task<OperationResult<PageResult<JogoResponse>>> ListarAsync(JogoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default)
    {
        var pagina = await _jogos.ListarAsync(filtro, paginacao, cancellationToken);
        return OperationResult<PageResult<JogoResponse>>.Success(pagina.Converter(j => j.ParaResponse()));
    }

    public async Task<OperationResult<JogoResponse>> ObterAsync(int id, CancellationToken cancellationToken = default)
    {
        var resultado = await _jogos.ObterPorIdAsync(id, cancellationToken);

        return resultado.IsFailure
            ? OperationResult<JogoResponse>.Failure(resultado.Error!)
            : OperationResult<JogoResponse>.Success(resultado.Data!.ParaResponse());
    }

    public async Task<OperationResult<JogoResponse>> CriarAsync(JogoRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var jogo = new Jogo(
                request.Titulo,
                request.Editora,
                request.Categoria,
                request.MinJogadores,
                request.MaxJogadores,
                request.DuracaoMediaMinutos,
                _relogio.GetUtcNow().UtcDateTime);

            await _jogos.AdicionarAsync(jogo, cancellationToken);
            return OperationResult<JogoResponse>.Success(jogo.ParaResponse());
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult<JogoResponse>.Failure(Error.De(excecao));
        }
    }

    public async Task<OperationResult<JogoResponse>> AtualizarAsync(int id, JogoRequest request, CancellationToken cancellationToken = default)
    {
        var resultado = await _jogos.ObterRastreadoPorIdAsync(id, cancellationToken);
        if (resultado.IsFailure)
            return OperationResult<JogoResponse>.Failure(resultado.Error!);

        try
        {
            var jogo = resultado.Data!;
            jogo.Atualizar(
                request.Titulo,
                request.Editora,
                request.Categoria,
                request.MinJogadores,
                request.MaxJogadores,
                request.DuracaoMediaMinutos);

            await _jogos.AtualizarAsync(jogo, cancellationToken);
            return OperationResult<JogoResponse>.Success(jogo.ParaResponse());
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult<JogoResponse>.Failure(Error.De(excecao));
        }
    }

    public async Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _jogos.RemoverAsync(id, cancellationToken);
            return OperationResult.Success();
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult.Failure(Error.De(excecao));
        }
    }
}
