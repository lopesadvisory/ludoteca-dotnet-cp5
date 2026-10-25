using System.ComponentModel.DataAnnotations;
using Ludoteca.Domain.Entities;

namespace Ludoteca.Application.Dtos;

public class EmprestimoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Informe o jogo que será emprestado.")]
    public int JogoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Informe o membro que fará o empréstimo.")]
    public int MembroId { get; set; }

    [Range(Emprestimo.PrazoMinimoDias, Emprestimo.PrazoMaximoDias, ErrorMessage = "O prazo deve ficar entre 1 e 30 dias.")]
    public int DiasDeEmprestimo { get; set; } = Emprestimo.PrazoPadraoDias;
}

public sealed record EmprestimoResponse(
    int Id,
    int JogoId,
    string JogoTitulo,
    int MembroId,
    string MembroNome,
    DateTime DataEmprestimo,
    DateTime DataPrevistaDevolucao,
    DateTime? DataDevolucao,
    string Status);
