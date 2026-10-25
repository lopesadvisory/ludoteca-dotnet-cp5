using Ludoteca.Api.Hateoas;
using Ludoteca.Application.Dtos;
using Ludoteca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ludoteca.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public class EnderecosController : ApiControllerBase
{
    private readonly IEnderecoService _enderecos;

    public EnderecosController(IEnderecoService enderecos)
    {
        _enderecos = enderecos;
    }

    /// <summary>Consulta um endereço pelo CEP no serviço externo ViaCEP.</summary>
    [HttpGet("{cep}")]
    [ProducesResponseType(typeof(Resource<EnderecoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ConsultarPorCep(string cep, CancellationToken cancellationToken)
    {
        var resultado = await _enderecos.ConsultarPorCepAsync(cep, cancellationToken);
        if (resultado.IsFailure)
            return Problema(resultado.Error!);

        var links = new List<Link>
        {
            NovoLink("self", "GET", nameof(ConsultarPorCep), valores: new { cep = resultado.Data!.Cep }),
            NovoLink("membros", "GET", nameof(MembrosController.Listar), "Membros")
        };

        return Ok(new Resource<EnderecoResponse>(resultado.Data!, links));
    }
}
