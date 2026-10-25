using Ludoteca.Api.Hateoas;
using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Application.Services;
using Ludoteca.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ludoteca.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class MembrosController : ApiControllerBase
{
    private readonly IMembroService _membros;

    public MembrosController(IMembroService membros)
    {
        _membros = membros;
    }

    /// <summary>Lista os membros com filtro por nome e paginação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResource<MembroResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? nome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.TamanhoPadrao,
        CancellationToken cancellationToken = default)
    {
        var paginacao = new PageRequest(page, pageSize);
        var resultado = await _membros.ListarAsync(new MembroFiltro(nome), paginacao, cancellationToken);

        string HrefDaPagina(int numero) =>
            Url.ActionLink(nameof(Listar), null, new { nome, page = numero, pageSize = paginacao.PageSize }) ?? string.Empty;

        return Ok(MontarColecao(resultado.Data!, ComLinks, HrefDaPagina));
    }

    /// <summary>Consulta um membro pelo identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Resource<MembroResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(int id, CancellationToken cancellationToken)
    {
        var resultado = await _membros.ObterAsync(id, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : Ok(ComLinks(resultado.Data!));
    }

    /// <summary>Cadastra um membro. O endereço é preenchido a partir do CEP pelo serviço ViaCEP.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Resource<MembroResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Criar([FromBody] MembroRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _membros.CriarAsync(request, cancellationToken);
        if (resultado.IsFailure)
            return Problema(resultado.Error!);

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Data!.Id }, ComLinks(resultado.Data));
    }

    /// <summary>Atualiza os dados de um membro.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Resource<MembroResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] MembroRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _membros.AtualizarAsync(id, request, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : Ok(ComLinks(resultado.Data!));
    }

    /// <summary>Remove um membro sem empréstimos registrados. Exige perfil Administrador.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(int id, CancellationToken cancellationToken)
    {
        var resultado = await _membros.RemoverAsync(id, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : NoContent();
    }

    private Resource<MembroResponse> ComLinks(MembroResponse membro)
    {
        var links = new List<Link>
        {
            NovoLink("self", "GET", nameof(ObterPorId), valores: new { id = membro.Id }),
            NovoLink("colecao", "GET", nameof(Listar)),
            NovoLink("atualizar", "PUT", nameof(Atualizar), valores: new { id = membro.Id }),
            NovoLink("emprestimos", "GET", nameof(EmprestimosController.Listar), "Emprestimos", new { membroId = membro.Id })
        };

        if (UsuarioEhAdministrador)
            links.Add(NovoLink("remover", "DELETE", nameof(Remover), valores: new { id = membro.Id }));

        return new Resource<MembroResponse>(membro, links);
    }
}
