using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ludoteca.Application.Abstractions.Security;
using Ludoteca.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ludoteca.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _opcoes;
    private readonly TimeProvider _relogio;

    public JwtTokenService(IOptions<JwtOptions> opcoes, TimeProvider relogio)
    {
        _opcoes = opcoes.Value;
        _relogio = relogio;
    }

    public TokenGerado Gerar(Usuario usuario)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var expiraEm = agora.AddMinutes(_opcoes.ExpiraEmMinutos);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Perfil.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.Chave));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opcoes.Emissor,
            audience: _opcoes.Audiencia,
            claims: claims,
            notBefore: agora,
            expires: expiraEm,
            signingCredentials: credenciais);

        return new TokenGerado(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}
