namespace Ludoteca.Application.Abstractions.Security;

public interface ISenhaHasher
{
    string GerarHash(string senha);

    bool Verificar(string hash, string senha);
}
