using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Tests.Unit.Support;
using Xunit;

namespace Ludoteca.Tests.Unit.Domain;

public class EmprestimoTests
{
    [Fact]
    public void Registrar_JogoDisponivel_MarcaJogoIndisponivelEDefinePrazo()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        var membro = Fabrica.NovoMembro();

        // Act
        var emprestimo = Emprestimo.Registrar(jogo, membro, 7, Fabrica.Agora);

        // Assert
        Assert.False(jogo.Disponivel);
        Assert.True(emprestimo.Ativo);
        Assert.Equal(Fabrica.Agora, emprestimo.DataEmprestimo);
        Assert.Equal(Fabrica.Agora.AddDays(7), emprestimo.DataPrevistaDevolucao);
    }

    [Fact]
    public void Registrar_JogoIndisponivel_LancaRegraDeNegocio()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        jogo.MarcarComoEmprestado();

        // Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => Emprestimo.Registrar(jogo, Fabrica.NovoMembro(), 7, Fabrica.Agora));

        // Assert
        Assert.Equal("Jogo.Indisponivel", excecao.Codigo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void Registrar_PrazoForaDoLimite_LancaRegraDeNegocioSemAlterarOJogo(int dias)
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();

        // Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => Emprestimo.Registrar(jogo, Fabrica.NovoMembro(), dias, Fabrica.Agora));

        // Assert
        Assert.Equal("Emprestimo.PrazoInvalido", excecao.Codigo);
        Assert.True(jogo.Disponivel);
    }

    [Fact]
    public void Devolver_EmprestimoAtivo_LiberaOJogoERegistraAData()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        var emprestimo = Emprestimo.Registrar(jogo, Fabrica.NovoMembro(), 7, Fabrica.Agora);
        var dataDaDevolucao = Fabrica.Agora.AddDays(3);

        // Act
        emprestimo.Devolver(dataDaDevolucao);

        // Assert
        Assert.False(emprestimo.Ativo);
        Assert.Equal(dataDaDevolucao, emprestimo.DataDevolucao);
        Assert.True(jogo.Disponivel);
    }

    [Fact]
    public void Devolver_EmprestimoJaDevolvido_LancaRegraDeNegocio()
    {
        // Arrange
        var emprestimo = Emprestimo.Registrar(Fabrica.NovoJogo(), Fabrica.NovoMembro(), 7, Fabrica.Agora);
        emprestimo.Devolver(Fabrica.Agora.AddDays(1));

        // Act
        var excecao = Assert.Throws<RegraDeNegocioException>(() => emprestimo.Devolver(Fabrica.Agora.AddDays(2)));

        // Assert
        Assert.Equal("Emprestimo.JaDevolvido", excecao.Codigo);
    }

    [Fact]
    public void EstaAtrasado_PrazoVencidoSemDevolucao_RetornaVerdadeiro()
    {
        // Arrange
        var emprestimo = Emprestimo.Registrar(Fabrica.NovoJogo(), Fabrica.NovoMembro(), 7, Fabrica.Agora);

        // Act
        var atrasado = emprestimo.EstaAtrasado(Fabrica.Agora.AddDays(8));

        // Assert
        Assert.True(atrasado);
    }

    [Fact]
    public void EstaAtrasado_DentroDoPrazo_RetornaFalso()
    {
        // Arrange
        var emprestimo = Emprestimo.Registrar(Fabrica.NovoJogo(), Fabrica.NovoMembro(), 7, Fabrica.Agora);

        // Act
        var atrasado = emprestimo.EstaAtrasado(Fabrica.Agora.AddDays(6));

        // Assert
        Assert.False(atrasado);
    }

    [Fact]
    public void EstaAtrasado_EmprestimoDevolvido_RetornaFalsoMesmoAposOPrazo()
    {
        // Arrange
        var emprestimo = Emprestimo.Registrar(Fabrica.NovoJogo(), Fabrica.NovoMembro(), 7, Fabrica.Agora);
        emprestimo.Devolver(Fabrica.Agora.AddDays(9));

        // Act
        var atrasado = emprestimo.EstaAtrasado(Fabrica.Agora.AddDays(20));

        // Assert
        Assert.False(atrasado);
    }
}
