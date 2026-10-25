namespace Ludoteca.Domain.Exceptions;

public class ConflitoDeDadosException : ErroDeDominioException
{
    public ConflitoDeDadosException(string codigo, string mensagem)
        : base(codigo, mensagem, TipoErro.Conflito)
    {
    }
}
