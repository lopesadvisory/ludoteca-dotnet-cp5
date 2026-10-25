namespace Ludoteca.Domain.Exceptions;

public abstract class ErroDeDominioException : Exception
{
    protected ErroDeDominioException(string codigo, string mensagem, TipoErro tipo)
        : base(mensagem)
    {
        Codigo = codigo;
        Tipo = tipo;
    }

    public string Codigo { get; }

    public TipoErro Tipo { get; }
}
