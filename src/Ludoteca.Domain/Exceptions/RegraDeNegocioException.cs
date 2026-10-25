namespace Ludoteca.Domain.Exceptions;

public class RegraDeNegocioException : ErroDeDominioException
{
    public RegraDeNegocioException(string codigo, string mensagem)
        : base(codigo, mensagem, TipoErro.RegraDeNegocio)
    {
    }
}
