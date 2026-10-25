using Ludoteca.Api.Hateoas;
using Ludoteca.Application.Dtos;
using Ludoteca.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ludoteca.Api.Controllers;

[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Autentica o usuário e devolve o token JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(Resource<TokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _auth.LoginAsync(request, cancellationToken);
        if (resultado.IsFailure)
            return Problema(resultado.Error!);

        var links = new List<Link>
        {
            NovoLink("jogos", "GET", nameof(JogosController.Listar), "Jogos"),
            NovoLink("membros", "GET", nameof(MembrosController.Listar), "Membros"),
            NovoLink("emprestimos", "GET", nameof(EmprestimosController.Listar), "Emprestimos")
        };

        return Ok(new Resource<TokenResponse>(resultado.Data!, links));
    }

    /// <summary>Cadastra um novo usuário com perfil Atendente.</summary>
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(Resource<UsuarioResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _auth.RegistrarAsync(request, cancellationToken);
        if (resultado.IsFailure)
            return Problema(resultado.Error!);

        var links = new List<Link> { NovoLink("login", "POST", nameof(Login)) };
        return StatusCode(StatusCodes.Status201Created, new Resource<UsuarioResponse>(resultado.Data!, links));
    }
}
