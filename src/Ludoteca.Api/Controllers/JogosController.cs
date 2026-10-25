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
public class JogosController : ApiControllerBase
{
    private readonly IJogoService _jogos;

    public JogosController(IJogoService jogos)
    {
        _jogos = jogos;
    }

    /// <summary>Lista os jogos com filtros opcionais e paginação.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionResource<JogoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] string? titulo,
        [FromQuery] string? categoria,
        [FromQuery] bool? disponivel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.TamanhoPadrao,
        CancellationToken cancellationToken = default)
    {
        var paginacao = new PageRequest(page, pageSize);
        var resultado = await _jogos.ListarAsync(new JogoFiltro(titulo, categoria, disponivel), paginacao, cancellationToken);

        string HrefDaPagina(int numero) =>
            Url.ActionLink(nameof(Listar), null, new { titulo, categoria, disponivel, page = numero, pageSize = paginacao.PageSize }) ?? string.Empty;

        return Ok(MontarColecao(resultado.Data!, ComLinks, HrefDaPagina));
    }

    /// <summary>Consulta um jogo pelo identificador.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Resource<JogoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(int id, CancellationToken cancellationToken)
    {
        var resultado = await _jogos.ObterAsync(id, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : Ok(ComLinks(resultado.Data!));
    }

    /// <summary>Cadastra um novo jogo. Exige perfil Administrador.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(typeof(Resource<JogoResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar([FromBody] JogoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _jogos.CriarAsync(request, cancellationToken);
        if (resultado.IsFailure)
            return Problema(resultado.Error!);

        return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Data!.Id }, ComLinks(resultado.Data));
    }

    /// <summary>Atualiza os dados de um jogo. Exige perfil Administrador.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(typeof(Resource<JogoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] JogoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _jogos.AtualizarAsync(id, request, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : Ok(ComLinks(resultado.Data!));
    }

    /// <summary>Remove um jogo sem empréstimos registrados. Exige perfil Administrador.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(PerfilUsuario.Administrador))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(int id, CancellationToken cancellationToken)
    {
        var resultado = await _jogos.RemoverAsync(id, cancellationToken);
        return resultado.IsFailure ? Problema(resultado.Error!) : NoContent();
    }

    private Resource<JogoResponse> ComLinks(JogoResponse jogo)
    {
        var links = new List<Link>
        {
            NovoLink("self", "GET", nameof(ObterPorId), valores: new { id = jogo.Id }),
            NovoLink("colecao", "GET", nameof(Listar))
        };

        if (jogo.Disponivel)
            links.Add(NovoLink("emprestar", "POST", nameof(EmprestimosController.Registrar), "Emprestimos"));

        if (UsuarioEhAdministrador)
        {
            links.Add(NovoLink("atualizar", "PUT", nameof(Atualizar), valores: new { id = jogo.Id }));
            links.Add(NovoLink("remover", "DELETE", nameof(Remover), valores: new { id = jogo.Id }));
        }

        return new Resource<JogoResponse>(jogo, links);
    }
}
