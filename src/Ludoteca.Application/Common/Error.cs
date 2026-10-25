using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Application.Common;

public sealed record Error(string Codigo, string Mensagem, TipoErro Tipo)
{
    public static Error De(ErroDeDominioException excecao) => new(excecao.Codigo, excecao.Message, excecao.Tipo);

    public static Error Validacao(string codigo, string mensagem) => new(codigo, mensagem, TipoErro.Validacao);

    public static Error NaoEncontrado(string codigo, string mensagem) => new(codigo, mensagem, TipoErro.NaoEncontrado);

    public static Error RegraDeNegocio(string codigo, string mensagem) => new(codigo, mensagem, TipoErro.RegraDeNegocio);

    public static Error NaoAutorizado(string codigo, string mensagem) => new(codigo, mensagem, TipoErro.NaoAutorizado);

    public static Error ServicoExterno(string codigo, string mensagem) => new(codigo, mensagem, TipoErro.ServicoExterno);
}
