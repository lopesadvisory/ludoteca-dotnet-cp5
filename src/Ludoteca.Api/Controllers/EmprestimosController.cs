using Ludoteca.Api.Hateoas;
using Ludoteca.Application;
using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ludoteca.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class EmprestimosController : ApiControllerBase
{
    private readonly IEmprestimoService _emprestimos;

    public EmprestimosController(IEmprestimoService emprestimos)
    {
        _emprestimos = emprestimos;
    }

    /// <summary>Lista os empréstimos com filtros opcionais e paginação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResource<EmprestimoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] int? membroId,
        [FromQuery] int? jogoId,
        [FromQuery] bool? ativos,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.TamanhoPadrao,
        CancellationToken cancellationToken = default)
    {
        var paginacao = new PageRequest(page, pageSize);
        var resultado = await _emprestimos.ListarAsync(new EmprestimoFiltro(membroId, jogoId, ativos), paginacao, cancellationToken);

        string HrefDaPagina(int numero) =>
            Url.ActionLink(nameof(Listar), null, new { membroId, jogoId, ativos, page = numero, pageSize = paginacao.PageSize }) ?? string.Empty;

        return Ok(MontarColecao(resultado.Data!, ComLinks, HrefDaPagina));
    }

    /// <summary>Consulta um empréstimo pelo identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Resource<EmprestimoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(int id, CancellationToken cancellationToken)
    {
        var resultado = await _emprestimos.ObterAsync(id, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : Ok(ComLinks(resultado.Data!));
    }

    /// <summary>Registra o empréstimo de um jogo disponível para um membro.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Resource<EmprestimoResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Registrar([FromBody] EmprestimoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _emprestimos.RegistrarAsync(request, cancellationToken);
        if (resultado.IsFailure)
            return Problema(resultado.Error!);

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Data!.Id }, ComLinks(resultado.Data));
    }

    /// <summary>Registra a devolução de um empréstimo ativo.</summary>
    [HttpPost("{id:int}/devolucao")]
    [ProducesResponseType(typeof(Resource<EmprestimoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Devolver(int id, CancellationToken cancellationToken)
    {
        var resultado = await _emprestimos.DevolverAsync(id, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : Ok(ComLinks(resultado.Data!));
    }

    private Resource<EmprestimoResponse> ComLinks(EmprestimoResponse emprestimo)
    {
        var links = new List<Link>
        {
            NovoLink("self", "GET", nameof(ObterPorId), valores: new { id = emprestimo.Id }),
            NovoLink("colecao", "GET", nameof(Listar)),
            NovoLink("jogo", "GET", nameof(JogosController.ObterPorId), "Jogos", new { id = emprestimo.JogoId }),
            NovoLink("membro", "GET", nameof(MembrosController.ObterPorId), "Membros", new { id = emprestimo.MembroId })
        };

        if (emprestimo.Status != Mapeamentos.StatusDevolvido)
            links.Add(NovoLink("devolver", "POST", nameof(Devolver), valores: new { id = emprestimo.Id }));

        return new Resource<EmprestimoResponse>(emprestimo, links);
    }
}
