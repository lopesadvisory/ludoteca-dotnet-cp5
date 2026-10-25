using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Tests.Unit.Support;
using Xunit;

namespace Ludoteca.Tests.Unit.Domain;

public class JogoTests
{
    [Fact]
    public void Construtor_DadosValidos_CriaJogoDisponivel()
    {
        // Arrange & Act
        var jogo = new Jogo("  Catan  ", "Devir", "Estratégia", 3, 4, 90, Fabrica.Agora);

        // Assert
        Assert.Equal("Catan", jogo.Titulo);
        Assert.True(jogo.Disponivel);
        Assert.Equal(Fabrica.Agora, jogo.CriadoEm);
    }

    [Theory]
    [InlineData("", "Devir", "Estratégia", "Jogo.TituloObrigatorio")]
    [InlineData("Catan", " ", "Estratégia", "Jogo.EditoraObrigatoria")]
    [InlineData("Catan", "Devir", "", "Jogo.CategoriaObrigatoria")]
    public void Construtor_CampoObrigatorioVazio_LancaRegraDeNegocio(string titulo, string editora, string categoria, string codigoEsperado)
    {
        // Arrange & Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => new Jogo(titulo, editora, categoria, 2, 4, 30, Fabrica.Agora));

        // Assert
        Assert.Equal(codigoEsperado, excecao.Codigo);
        Assert.Equal(TipoErro.RegraDeNegocio, excecao.Tipo);
    }

    [Fact]
    public void Construtor_MaximoMenorQueMinimo_LancaRegraDeNegocio()
    {
        // Arrange & Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => new Jogo("Catan", "Devir", "Estratégia", 5, 2, 90, Fabrica.Agora));

        // Assert
        Assert.Equal("Jogo.MaxJogadoresInvalido", excecao.Codigo);
    }

    [Fact]
    public void Construtor_MinimoDeJogadoresZero_LancaRegraDeNegocio()
    {
        // Arrange & Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => new Jogo("Catan", "Devir", "Estratégia", 0, 2, 90, Fabrica.Agora));

        // Assert
        Assert.Equal("Jogo.MinJogadoresInvalido", excecao.Codigo);
    }

    [Fact]
    public void Construtor_DuracaoZero_LancaRegraDeNegocio()
    {
        // Arrange & Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => new Jogo("Catan", "Devir", "Estratégia", 2, 4, 0, Fabrica.Agora));

        // Assert
        Assert.Equal("Jogo.DuracaoInvalida", excecao.Codigo);
    }

    [Fact]
    public void MarcarComoEmprestado_JogoDisponivel_TornaOJogoIndisponivel()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();

        // Act
        jogo.MarcarComoEmprestado();

        // Assert
        Assert.False(jogo.Disponivel);
    }

    [Fact]
    public void MarcarComoEmprestado_JogoJaEmprestado_LancaRegraDeNegocio()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        jogo.MarcarComoEmprestado();

        // Act
        var excecao = Assert.Throws<RegraDeNegocioException>(() => jogo.MarcarComoEmprestado());

        // Assert
        Assert.Equal("Jogo.Indisponivel", excecao.Codigo);
    }

    [Fact]
    public void MarcarComoDisponivel_JogoEmprestado_TornaOJogoDisponivel()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        jogo.MarcarComoEmprestado();

        // Act
        jogo.MarcarComoDisponivel();

        // Assert
        Assert.True(jogo.Disponivel);
    }

    [Fact]
    public void Atualizar_DadosValidos_SubstituiOsCampos()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();

        // Act
        jogo.Atualizar("Carcassonne", "Galápagos", "Família", 2, 5, 45);

        // Assert
        Assert.Equal("Carcassonne", jogo.Titulo);
        Assert.Equal("Galápagos", jogo.Editora);
        Assert.Equal(5, jogo.MaxJogadores);
    }
}
