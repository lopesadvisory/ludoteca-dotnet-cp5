using Ludoteca.Domain.Entities;

namespace Ludoteca.Tests.Unit.Support;

internal static class Fabrica
{
    public static readonly DateTime Agora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    public static Jogo NovoJogo(string titulo = "Catan")
    {
        return new Jogo(titulo, "Devir", "Estratégia", 3, 4, 90, Agora);
    }

    public static Membro NovoMembro(string email = "maria@teste.com")
    {
        var membro = new Membro("Maria Souza", email, "11999990000", Agora);
        membro.DefinirEndereco("01001000", "Praça da Sé", "Sé", "São Paulo", "SP");
        return membro;
    }
}
