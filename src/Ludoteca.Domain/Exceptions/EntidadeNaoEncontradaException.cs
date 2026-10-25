namespace Ludoteca.Domain.Exceptions;

public class EntidadeNaoEncontradaException : ErroDeDominioException
{
    public EntidadeNaoEncontradaException(string codigo, string mensagem)
        : base(codigo, mensagem, TipoErro.NaoEncontrado)
    {
    }
}
