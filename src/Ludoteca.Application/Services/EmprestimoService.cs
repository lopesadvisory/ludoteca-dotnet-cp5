using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Application.Services;

public class EmprestimoService : IEmprestimoService
{
    private readonly IEmprestimoRepository _emprestimos;
    private readonly IJogoRepository _jogos;
    private readonly IMembroRepository _membros;
    private readonly TimeProvider _relogio;

    public EmprestimoService(
        IEmprestimoRepository emprestimos,
        IJogoRepository jogos,
        IMembroRepository membros,
        TimeProvider relogio)
    {
        _emprestimos = emprestimos;
        _jogos = jogos;
        _membros = membros;
        _relogio = relogio;
    }

    public async Task<OperationResult<PageResult<EmprestimoResponse>>> ListarAsync(EmprestimoFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default)
    {
        var agora = Agora();
        var pagina = await _emprestimos.ListarAsync(filtro, paginacao, cancellationToken);
        return OperationResult<PageResult<EmprestimoResponse>>.Success(pagina.Converter(e => e.ParaResponse(agora)));
    }

    public async Task<OperationResult<EmprestimoResponse>> ObterAsync(int id, CancellationToken cancellationToken = default)
    {
        var resultado = await _emprestimos.ObterDetalheAsync(id, cancellationToken);

        return resultado.IsFailure
            ? OperationResult<EmprestimoResponse>.Failure(resultado.Error!)
            : OperationResult<EmprestimoResponse>.Success(resultado.Data!.ParaResponse(Agora()));
    }

    public async Task<OperationResult<EmprestimoResponse>> RegistrarAsync(EmprestimoRequest request, CancellationToken cancellationToken = default)
    {
        var jogo = await _jogos.ObterRastreadoPorIdAsync(request.JogoId, cancellationToken);
        if (jogo.IsFailure)
            return OperationResult<EmprestimoResponse>.Failure(jogo.Error!);

        var membro = await _membros.ObterRastreadoPorIdAsync(request.MembroId, cancellationToken);
        if (membro.IsFailure)
            return OperationResult<EmprestimoResponse>.Failure(membro.Error!);

        var ativos = await _emprestimos.ContarAtivosPorMembroAsync(request.MembroId, cancellationToken);
        if (ativos >= Emprestimo.LimiteAtivosPorMembro)
        {
            return OperationResult<EmprestimoResponse>.Failure(Error.RegraDeNegocio(
                "Emprestimo.LimiteExcedido",
                $"O membro já possui {Emprestimo.LimiteAtivosPorMembro} empréstimos ativos."));
        }

        try
        {
            var agora = Agora();
            var emprestimo = Emprestimo.Registrar(jogo.Data!, membro.Data!, request.DiasDeEmprestimo, agora);

            await _emprestimos.AdicionarAsync(emprestimo, cancellationToken);
            return OperationResult<EmprestimoResponse>.Success(emprestimo.ParaResponse(agora));
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult<EmprestimoResponse>.Failure(Error.De(excecao));
        }
    }

    public async Task<OperationResult<EmprestimoResponse>> DevolverAsync(int id, CancellationToken cancellationToken = default)
    {
        var resultado = await _emprestimos.ObterRastreadoPorIdAsync(id, cancellationToken);
        if (resultado.IsFailure)
            return OperationResult<EmprestimoResponse>.Failure(resultado.Error!);

        try
        {
            var agora = Agora();
            var emprestimo = resultado.Data!;
            emprestimo.Devolver(agora);

            await _emprestimos.AtualizarAsync(emprestimo, cancellationToken);
            return OperationResult<EmprestimoResponse>.Success(emprestimo.ParaResponse(agora));
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult<EmprestimoResponse>.Failure(Error.De(excecao));
        }
    }

    private DateTime Agora() => _relogio.GetUtcNow().UtcDateTime;
}
