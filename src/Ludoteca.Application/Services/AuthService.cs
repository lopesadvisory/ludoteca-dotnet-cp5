using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Abstractions.Security;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Enums;
using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly ISenhaHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly TimeProvider _relogio;

    public AuthService(IUsuarioRepository usuarios, ISenhaHasher hasher, ITokenService tokens, TimeProvider relogio)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _tokens = tokens;
        _relogio = relogio;
    }

    public async Task<OperationResult<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var resultado = await _usuarios.ObterPorEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

        // A mesma mensagem vale para e-mail desconhecido e senha errada, para não revelar quais e-mails existem.
        if (resultado.IsFailure || !_hasher.Verificar(resultado.Data!.SenhaHash, request.Senha))
            return OperationResult<TokenResponse>.Failure(Error.NaoAutorizado("Auth.CredenciaisInvalidas", "E-mail ou senha inválidos."));

        var usuario = resultado.Data!;
        var token = _tokens.Gerar(usuario);
        return OperationResult<TokenResponse>.Success(new TokenResponse(token.Token, token.ExpiraEm, usuario.ParaResponse()));
    }

    public async Task<OperationResult<UsuarioResponse>> RegistrarAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var usuario = new Usuario(
                request.Nome,
                request.Email,
                _hasher.GerarHash(request.Senha),
                PerfilUsuario.Atendente,
                _relogio.GetUtcNow().UtcDateTime);

            await _usuarios.AdicionarAsync(usuario, cancellationToken);
            return OperationResult<UsuarioResponse>.Success(usuario.ParaResponse());
        }
        catch (ErroDeDominioException excecao)
        {
            return OperationResult<UsuarioResponse>.Failure(Error.De(excecao));
        }
    }
}
