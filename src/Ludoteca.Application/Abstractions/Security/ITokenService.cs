using Ludoteca.Domain.Entities;

namespace Ludoteca.Application.Abstractions.Security;

public sealed record TokenGerado(string Token, DateTime ExpiraEm);

public interface ITokenService
{
    TokenGerado Gerar(Usuario usuario);
}
