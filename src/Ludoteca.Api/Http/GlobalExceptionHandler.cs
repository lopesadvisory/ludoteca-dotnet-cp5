using Ludoteca.Application.Common;
using Ludoteca.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Ludoteca.Api.Http;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problema;

        if (exception is ErroDeDominioException erroDeDominio)
        {
            problema = ProblemaHttp.Criar(Error.De(erroDeDominio));
        }
        else
        {
            _logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);
            problema = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno",
                Detail = "Ocorreu um erro inesperado ao processar a requisição."
            };
        }

        httpContext.Response.StatusCode = problema.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);
        return true;
    }
}
