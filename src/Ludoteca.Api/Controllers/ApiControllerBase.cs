using Ludoteca.Api.Hateoas;
using Ludoteca.Api.Http;
using Ludoteca.Application.Common;
using Ludoteca.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Ludoteca.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected bool UsuarioEhAdministrador => User.IsInRole(nameof(PerfilUsuario.Administrador));

    protected IActionResult Problema(Error erro)
    {
        var problema = ProblemaHttp.Criar(erro);
        return new ObjectResult(problema) { StatusCode = problema.Status };
    }

    protected Link NovoLink(string rel, string metodo, string acao, string? controller = null, object? valores = null)
    {
        var nomeDoController = controller ?? ControllerContext.ActionDescriptor.ControllerName;
        var href = Url.ActionLink(acao, nomeDoController, valores) ?? string.Empty;
        return new Link(rel, href, metodo);
    }

    protected CollectionResource<T> MontarColecao<T>(
        PageResult<T> pagina,
        Func<T, Resource<T>> comLinks,
        Func<int, string> hrefDaPagina)
    {
        var links = new List<Link>
        {
            new("self", hrefDaPagina(pagina.Page), "GET"),
            new("first", hrefDaPagina(1), "GET")
        };

        if (pagina.HasPrevious)
            links.Add(new Link("prev", hrefDaPagina(pagina.Page - 1), "GET"));

        if (pagina.HasNext)
            links.Add(new Link("next", hrefDaPagina(pagina.Page + 1), "GET"));

        links.Add(new Link("last", hrefDaPagina(Math.Max(pagina.TotalPages, 1)), "GET"));

        var itens = pagina.Data.Select(comLinks).ToList();
        return new CollectionResource<T>(itens, PaginationInfo.De(pagina), links);
    }
}
