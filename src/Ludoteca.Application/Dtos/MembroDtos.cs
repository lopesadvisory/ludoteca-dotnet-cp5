using System.ComponentModel.DataAnnotations;

namespace Ludoteca.Application.Dtos;

public class MembroRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 120 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(160, ErrorMessage = "O e-mail deve ter no máximo 160 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(25, ErrorMessage = "O telefone deve ter no máximo 25 caracteres.")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "O CEP é obrigatório.")]
    [RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "CEP inválido. Use o formato 00000-000.")]
    public string Cep { get; set; } = string.Empty;
}

public sealed record EnderecoResponse(
    string Cep,
    string? Logradouro,
    string? Bairro,
    string Cidade,
    string Uf);

public sealed record MembroResponse(
    int Id,
    string Nome,
    string Email,
    string? Telefone,
    EnderecoResponse Endereco,
    DateTime CriadoEm);
