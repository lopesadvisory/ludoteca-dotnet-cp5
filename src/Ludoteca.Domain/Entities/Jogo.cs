using Ludoteca.Domain.Exceptions;

namespace Ludoteca.Domain.Entities;

public class Jogo
{
    private Jogo()
    {
    }

    public Jogo(string titulo, string editora, string categoria, int minJogadores, int maxJogadores, int duracaoMediaMinutos, DateTime criadoEm)
    {
        Preencher(titulo, editora, categoria, minJogadores, maxJogadores, duracaoMediaMinutos);
        Disponivel = true;
        CriadoEm = criadoEm;
    }

    public int Id { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Editora { get; private set; } = string.Empty;

    public string Categoria { get; private set; } = string.Empty;

    public int MinJogadores { get; private set; }

    public int MaxJogadores { get; private set; }

    public int DuracaoMediaMinutos { get; private set; }

    public bool Disponivel { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public void Atualizar(string titulo, string editora, string categoria, int minJogadores, int maxJogadores, int duracaoMediaMinutos)
    {
        Preencher(titulo, editora, categoria, minJogadores, maxJogadores, duracaoMediaMinutos);
    }

    public void MarcarComoEmprestado()
    {
        if (!Disponivel)
            throw new RegraDeNegocioException("Jogo.Indisponivel", $"O jogo '{Titulo}' já está emprestado.");

        Disponivel = false;
    }

    public void MarcarComoDisponivel()
    {
        Disponivel = true;
    }

    private void Preencher(string titulo, string editora, string categoria, int minJogadores, int maxJogadores, int duracaoMediaMinutos)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new RegraDeNegocioException("Jogo.TituloObrigatorio", "O título do jogo é obrigatório.");

        if (string.IsNullOrWhiteSpace(editora))
            throw new RegraDeNegocioException("Jogo.EditoraObrigatoria", "A editora do jogo é obrigatória.");

        if (string.IsNullOrWhiteSpace(categoria))
            throw new RegraDeNegocioException("Jogo.CategoriaObrigatoria", "A categoria do jogo é obrigatória.");

        if (minJogadores < 1)
            throw new RegraDeNegocioException("Jogo.MinJogadoresInvalido", "O número mínimo de jogadores deve ser pelo menos 1.");

        if (maxJogadores < minJogadores)
            throw new RegraDeNegocioException("Jogo.MaxJogadoresInvalido", "O número máximo de jogadores não pode ser menor que o mínimo.");

        if (duracaoMediaMinutos < 1)
            throw new RegraDeNegocioException("Jogo.DuracaoInvalida", "A duração média deve ser de pelo menos 1 minuto.");

        Titulo = titulo.Trim();
        Editora = editora.Trim();
        Categoria = categoria.Trim();
        MinJogadores = minJogadores;
        MaxJogadores = maxJogadores;
        DuracaoMediaMinutos = duracaoMediaMinutos;
    }
}
