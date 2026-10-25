using Ludoteca.Application.Abstractions.External;
using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Application.Services;

public class MembroService : IMembroService
{
    private readonly IMembroRepository _membros;
    private readonly ICepService _cepService;
    private readonly TimeProvider _relogio;

    public MembroService(IMembroRepository membros, ICepService cepService, TimeProvider relogio)
    {
        _membros = membros;
        _cepService = cepService;
        _relogio = relogio;
    }

    public async Task<OperationResult<PageResult<MembroResponse>>> ListarAsync(MembroFiltro filtro, PageRequest paginacao, CancellationToken cancellationToken = default)
    {
        var pagina = await _membros.ListarAsync(filtro, paginacao, cancellationToken);
        return OperationResult<PageResult<MembroResponse>>.Success(pagina.Converter(m => m.ParaResponse()));
    }

    public async Task<OperationResult<MembroResponse>> ObterAsync(int id, CancellationToken cancellationToken = default)
    {
        var resultado = await _membros.ObterPorIdAsync(id, cancellationToken);

        return resultado.IsFailure
            ? OperationResult<MembroResponse>.Failure(resultado.Error!)
            : OperationResult<MembroResponse>.Success(resultado.Data!.ParaResponse());
    }

    public async Task<OperationResult<MembroResponse>> CriarAsync(MembroRequest request, CancellationToken cancellationToken = default)
    {
        var cep = Cep.Normalizar(request.Cep);
        if (cep is null)
            return OperationResult<MembroResponse>.Failure(Error.Validacao("Membro.CepInvalido", "CEP inválido. Informe 8 dígitos."));

        var endereco = await _cepService.ConsultarAsync(cep, cancellationToken);
        if (endereco.IsFailure)
            return OperationResult<MembroResponse>.Failure(endereco.Error!);

        try
        {
            var membro = new Membro(request.Nome, request.Email, request.Telefone, _relogio.GetUtcNow().UtcDateTime);
            DefinirEndereco(membro, endereco.Data!);

            await _membros.AdicionarAsync(membro, cancellationToken);
            return OperationResult<MembroResponse>.Success(membro.ParaResponse());
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult<MembroResponse>.Failure(Error.De(excecao));
        }
    }

    public async Task<OperationResult<MembroResponse>> AtualizarAsync(int id, MembroRequest request, CancellationToken cancellationToken = default)
    {
        var resultado = await _membros.ObterRastreadoPorIdAsync(id, cancellationToken);
        if (resultado.IsFailure)
            return OperationResult<MembroResponse>.Failure(resultado.Error!);

        var cep = Cep.Normalizar(request.Cep);
        if (cep is null)
            return OperationResult<MembroResponse>.Failure(Error.Validacao("Membro.CepInvalido", "CEP inválido. Informe 8 dígitos."));

        var membro = resultado.Data!;

        // O endereço só é consultado de novo quando o CEP muda.
        EnderecoResponse? novoEndereco = null;
        if (cep != membro.Cep)
        {
            var consulta = await _cepService.ConsultarAsync(cep, cancellationToken);
            if (consulta.IsFailure)
                return OperationResult<MembroResponse>.Failure(consulta.Error!);

            novoEndereco = consulta.Data!;
        }

        try
        {
            membro.AtualizarDados(request.Nome, request.Email, request.Telefone);
            if (novoEndereco is not null)
                DefinirEndereco(membro, novoEndereco);

            await _membros.AtualizarAsync(membro, cancellationToken);
            return OperationResult<MembroResponse>.Success(membro.ParaResponse());
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult<MembroResponse>.Failure(Error.De(excecao));
        }
    }

    public async Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _membros.RemoverAsync(id, cancellationToken);
            return OperationResult.Success();
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult.Failure(Error.De(excecao));
        }
    }

    private static void DefinirEndereco(Membro membro, EnderecoResponse endereco)
    {
        membro.DefinirEndereco(endereco.Cep, endereco.Logradouro, endereco.Bairro, endereco.Cidade, endereco.Uf);
    }
}
