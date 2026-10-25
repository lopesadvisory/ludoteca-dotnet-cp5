using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Domain.Entities;

public class Emprestimo
{
    public const int LimiteAtivosPorMembro = 3;
    public const int PrazoMinimoDias = 1;
    public const int PrazoMaximoDias = 30;
    public const int PrazoPadraoDias = 7;

    private Emprestimo()
    {
    }

    public int Id { get; private set; }

    public int JogoId { get; private set; }

    public Jogo Jogo { get; private set; } = null!;

    public int MembroId { get; private set; }

    public Membro Membro { get; private set; } = null!;

    public DateTime DataEmprestimo { get; private set; }

    public DateTime DataPrevistaDevolucao { get; private set; }

    public DateTime? DataDevolucao { get; private set; }

    public bool Ativo => DataDevolucao is null;

    public static Emprestimo Registrar(Jogo jogo, Membro membro, int diasDeEmprestimo, DateTime agora)
    {
        if (diasDeEmprestimo < PrazoMinimoDias || diasDeEmprestimo > PrazoMaximoDias)
            throw new RegraDeNegocioException(
                "Emprestimo.PrazoInvalido",
                $"O prazo do empréstimo deve ficar entre {PrazoMinimoDias} e {PrazoMaximoDias} dias.");

        jogo.MarcarComoEmprestado();

        return new Emprestimo
        {
            Jogo = jogo,
            JogoId = jogo.Id,
            Membro = membro,
            MembroId = membro.Id,
            DataEmprestimo = agora,
            DataPrevistaDevolucao = agora.AddDays(diasDeEmprestimo)
        };
    }

    public void Devolver(DateTime agora)
    {
        if (!Ativo)
            throw new RegraDeNegocioException("Emprestimo.JaDevolvido", "Este empréstimo já foi devolvido.");

        DataDevolucao = agora;
        Jogo.MarcarComoDisponivel();
    }

    public bool EstaAtrasado(DateTime agora)
    {
        return Ativo && agora > DataPrevistaDevolucao;
    }
}
