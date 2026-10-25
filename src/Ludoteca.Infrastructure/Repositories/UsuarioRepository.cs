using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ludoteca.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly LudotecaDbContext _context;

    public UsuarioRepository(LudotecaDbContext context)
    {
        _context = context;
    }

    public async Task<OperationResult<Usuario>> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        return usuario is null
            ? OperationResult<Usuario>.Failure(Error.NaoEncontrado("Usuario.NaoEncontrado", "Usuário não encontrado."))
            : OperationResult<Usuario>.Success(usuario);
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email, cancellationToken))
            throw EmailDuplicado(usuario.Email);

        _context.Usuarios.Add(usuario);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (ErrosDeBanco.EhViolacaoDeUnicidade(excecao))
        {
            throw EmailDuplicado(usuario.Email);
        }
    }

    private static ConflitoDeDadosException EmailDuplicado(string email)
    {
        return new ConflitoDeDadosException("Usuario.EmailDuplicado", $"Já existe um usuário cadastrado com o e-mail '{email}'.");
    }
}
