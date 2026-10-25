using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Domain.Entities;

public class Membro
{
    private Membro()
    {
    }

    public Membro(string nome, string email, string? telefone, DateTime criadoEm)
    {
        PreencherDados(nome, email, telefone);
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? Telefone { get; private set; }

    public string Cep { get; private set; } = string.Empty;

    public string? Logradouro { get; private set; }

    public string? Bairro { get; private set; }

    public string Cidade { get; private set; } = string.Empty;

    public string Uf { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }

    public void AtualizarDados(string nome, string email, string? telefone)
    {
        PreencherDados(nome, email, telefone);
    }

    public void DefinirEndereco(string cep, string? logradouro, string? bairro, string cidade, string uf)
    {
        if (string.IsNullOrWhiteSpace(cep))
            throw new RegraDeNegocioException("Membro.CepObrigatorio", "O CEP do membro é obrigatório.");

        if (string.IsNullOrWhiteSpace(cidade) || string.IsNullOrWhiteSpace(uf))
            throw new RegraDeNegocioException("Membro.EnderecoIncompleto", "O endereço do membro precisa ter cidade e UF.");

        Cep = cep;
        Logradouro = string.IsNullOrWhiteSpace(logradouro) ? null : logradouro.Trim();
        Bairro = string.IsNullOrWhiteSpace(bairro) ? null : bairro.Trim();
        Cidade = cidade.Trim();
        Uf = uf.Trim().ToUpperInvariant();
    }

    private void PreencherDados(string nome, string email, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("Membro.NomeObrigatorio", "O nome do membro é obrigatório.");

        if (string.IsNullOrWhiteSpace(email))
            throw new RegraDeNegocioException("Membro.EmailObrigatorio", "O e-mail do membro é obrigatório.");

        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
    }
}
