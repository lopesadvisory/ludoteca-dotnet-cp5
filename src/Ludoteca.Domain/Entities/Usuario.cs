using Ludoteca.Domain.Enums;
using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Domain.Entities;

public class Usuario
{
    private Usuario()
    {
    }

    public Usuario(string nome, string email, string senhaHash, PerfilUsuario perfil, DateTime criadoEm)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("Usuario.NomeObrigatorio", "O nome do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new RegraDeNegocioException("Usuario.EmailObrigatorio", "O e-mail do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            throw new RegraDeNegocioException("Usuario.SenhaObrigatoria", "A senha do usuário é obrigatória.");

        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
        Perfil = perfil;
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string SenhaHash { get; private set; } = string.Empty;

    public PerfilUsuario Perfil { get; private set; }

    public DateTime CriadoEm { get; private set; }
}
