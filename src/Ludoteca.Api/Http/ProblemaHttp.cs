using Ludoteca.Application.Common;
using Ludoteca.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Ludoteca.Api.Http;

public static class ProblemaHttp
{
    public static ProblemDetails Criar(Error erro)
    {
        var (status, titulo) = erro.Tipo switch
        {
            TipoErro.Validacao => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            TipoErro.NaoAutorizado => (StatusCodes.Status401Unauthorized, "Não autorizado"),
            TipoErro.NaoEncontrado => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            TipoErro.Conflito => (StatusCodes.Status409Conflict, "Conflito de dados"),
            TipoErro.RegraDeNegocio => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            TipoErro.ServicoExterno => (StatusCodes.Status503ServiceUnavailable, "Serviço externo indisponível"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = erro.Mensagem
        };
        problema.Extensions["codigo"] = erro.Codigo;

        return problema;
    }
}
