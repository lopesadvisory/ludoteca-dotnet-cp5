using System.ComponentModel.DataAnnotations;

namespace Ludoteca.Application.Dtos;

public class JogoRequest
{
    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O título deve ter entre 2 e 120 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A editora é obrigatória.")]
    [StringLength(80, ErrorMessage = "A editora deve ter no máximo 80 caracteres.")]
    public string Editora { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    [StringLength(40, ErrorMessage = "A categoria deve ter no máximo 40 caracteres.")]
    public string Categoria { get; set; } = string.Empty;

    [Range(1, 50, ErrorMessage = "O mínimo de jogadores deve ficar entre 1 e 50.")]
    public int MinJogadores { get; set; }

    [Range(1, 50, ErrorMessage = "O máximo de jogadores deve ficar entre 1 e 50.")]
    public int MaxJogadores { get; set; }

    [Range(1, 1440, ErrorMessage = "A duração média deve ficar entre 1 e 1440 minutos.")]
    public int DuracaoMediaMinutos { get; set; }
}

public sealed record JogoResponse(
    int Id,
    string Titulo,
    string Editora,
    string Categoria,
    int MinJogadores,
    int MaxJogadores,
    int DuracaoMediaMinutos,
    bool Disponivel,
    DateTime CriadoEm);
