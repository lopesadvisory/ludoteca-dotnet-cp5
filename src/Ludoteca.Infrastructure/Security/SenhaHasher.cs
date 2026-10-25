using Ludoteca.Application.Abstractions.Security;
using Ludoteca.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Ludoteca.Infrastructure.Security;

public class SenhaHasher : ISenhaHasher
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    // O PasswordHasher não usa a instância do usuário, por isso passamos null.
    public string GerarHash(string senha)
    {
        return _hasher.HashPassword(null!, senha);
    }

    public bool Verificar(string hash, string senha)
    {
        return _hasher.VerifyHashedPassword(null!, hash, senha) != PasswordVerificationResult.Failed;
    }
}
