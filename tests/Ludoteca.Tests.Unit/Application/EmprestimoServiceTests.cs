using Ludoteca.Application;
using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Application.Services;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Tests.Unit.Support;
using Moq;
using Xunit;

namespace Ludoteca.Tests.Unit.Application;

public class EmprestimoServiceTests
{
    private readonly Mock<IEmprestimoRepository> _emprestimos = new();
    private readonly Mock<IJogoRepository> _jogos = new();
    private readonly Mock<IMembroRepository> _membros = new();

    private EmprestimoService CriarServico() =>
        new(_emprestimos.Object, _jogos.Object, _membros.Object, new RelogioFixo(Fabrica.Agora));

    private static EmprestimoRequest RequestValido() => new() { JogoId = 1, MembroId = 2, DiasDeEmprestimo = 7 };

    private void ConfigurarJogoEMembro(Jogo jogo, Membro membro)
    {
        _jogos
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Jogo>.Success(jogo));
        _membros
            .Setup(r => r.ObterRastreadoPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Membro>.Success(membro));
    }

    [Fact]
    public async Task RegistrarAsync_JogoDisponivel_RegistraEMarcaOJogoIndisponivel()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        ConfigurarJogoEMembro(jogo, Fabrica.NovoMembro());
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(Mapeamentos.StatusAtivo, resultado.Data!.Status);
        Assert.Equal("Catan", resultado.Data.JogoTitulo);
        Assert.False(jogo.Disponivel);
        _emprestimos.Verify(r => r.AdicionarAsync(It.IsAny<Emprestimo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_JogoInexistente_RetornaFalhaNaoEncontrado()
    {
        // Arrange
        _jogos
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Jogo>.Failure(Error.NaoEncontrado("Jogo.NaoEncontrado", "Jogo 1 não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Jogo.NaoEncontrado", resultado.Error!.Codigo);
        _emprestimos.Verify(r => r.AdicionarAsync(It.IsAny<Emprestimo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegistrarAsync_MembroInexistente_RetornaFalhaNaoEncontrado()
    {
        // Arrange
        _jogos
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Jogo>.Success(Fabrica.NovoJogo()));
        _membros
            .Setup(r => r.ObterRastreadoPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Membro>.Failure(Error.NaoEncontrado("Membro.NaoEncontrado", "Membro 2 não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Membro.NaoEncontrado", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task RegistrarAsync_MembroNoLimiteDeEmprestimosAtivos_RetornaFalhaDeRegraDeNegocio()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        ConfigurarJogoEMembro(jogo, Fabrica.NovoMembro());
        _emprestimos
            .Setup(r => r.ContarAtivosPorMembroAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Emprestimo.LimiteAtivosPorMembro);
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Emprestimo.LimiteExcedido", resultado.Error!.Codigo);
        Assert.Equal(TipoErro.RegraDeNegocio, resultado.Error.Tipo);
        Assert.True(jogo.Disponivel);
    }

    [Fact]
    public async Task RegistrarAsync_JogoJaEmprestado_RetornaFalhaDeRegraDeNegocio()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        jogo.MarcarComoEmprestado();
        ConfigurarJogoEMembro(jogo, Fabrica.NovoMembro());
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Jogo.Indisponivel", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task RegistrarAsync_RepositorioLancaConflitoDeConcorrencia_RetornaFalhaDeConflito()
    {
        // Arrange
        ConfigurarJogoEMembro(Fabrica.NovoJogo(), Fabrica.NovoMembro());
        _emprestimos
            .Setup(r => r.AdicionarAsync(It.IsAny<Emprestimo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflitoDeDadosException("Emprestimo.ConflitoDeConcorrencia", "Jogo alterado por outra operação."));
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Conflito, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task DevolverAsync_EmprestimoAtivo_LiberaOJogoERetornaStatusDevolvido()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        var emprestimo = Emprestimo.Registrar(jogo, Fabrica.NovoMembro(), 7, Fabrica.Agora.AddDays(-2));
        _emprestimos
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Emprestimo>.Success(emprestimo));
        var servico = CriarServico();

        // Act
        var resultado = await servico.DevolverAsync(1);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(Mapeamentos.StatusDevolvido, resultado.Data!.Status);
        Assert.True(jogo.Disponivel);
        _emprestimos.Verify(r => r.AtualizarAsync(emprestimo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DevolverAsync_EmprestimoJaDevolvido_RetornaFalhaDeRegraDeNegocio()
    {
        // Arrange
        var emprestimo = Emprestimo.Registrar(Fabrica.NovoJogo(), Fabrica.NovoMembro(), 7, Fabrica.Agora.AddDays(-2));
        emprestimo.Devolver(Fabrica.Agora.AddDays(-1));
        _emprestimos
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Emprestimo>.Success(emprestimo));
        var servico = CriarServico();

        // Act
        var resultado = await servico.DevolverAsync(1);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Emprestimo.JaDevolvido", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task DevolverAsync_EmprestimoInexistente_RetornaFalhaNaoEncontrado()
    {
        // Arrange
        _emprestimos
            .Setup(r => r.ObterRastreadoPorIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Emprestimo>.Failure(Error.NaoEncontrado("Emprestimo.NaoEncontrado", "Empréstimo 99 não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.DevolverAsync(99);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ListarAsync_EmprestimoComPrazoVencido_RetornaStatusAtrasado()
    {
        // Arrange
        var atrasado = new EmprestimoDetalhe(1, 1, "Catan", 2, "Maria Souza", Fabrica.Agora.AddDays(-10), Fabrica.Agora.AddDays(-3), null);
        var noPrazo = new EmprestimoDetalhe(2, 3, "Azul", 2, "Maria Souza", Fabrica.Agora.AddDays(-1), Fabrica.Agora.AddDays(6), null);
        var devolvido = new EmprestimoDetalhe(3, 4, "Uno", 2, "Maria Souza", Fabrica.Agora.AddDays(-9), Fabrica.Agora.AddDays(-2), Fabrica.Agora.AddDays(-4));
        _emprestimos
            .Setup(r => r.ListarAsync(It.IsAny<EmprestimoFiltro>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageResult<EmprestimoDetalhe>(new[] { atrasado, noPrazo, devolvido }, 1, 10, 3));
        var servico = CriarServico();

        // Act
        var resultado = await servico.ListarAsync(new EmprestimoFiltro(null, null, null), new PageRequest(1, 10));

        // Assert
        var status = resultado.Data!.Data.Select(e => e.Status).ToArray();
        Assert.Equal(new[] { Mapeamentos.StatusAtrasado, Mapeamentos.StatusAtivo, Mapeamentos.StatusDevolvido }, status);
    }

    [Fact]
    public async Task ObterAsync_EmprestimoInexistente_RetornaFalhaNaoEncontrado()
    {
        // Arrange
        _emprestimos
            .Setup(r => r.ObterDetalheAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<EmprestimoDetalhe>.Failure(Error.NaoEncontrado("Emprestimo.NaoEncontrado", "Empréstimo 99 não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.ObterAsync(99);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Emprestimo.NaoEncontrado", resultado.Error!.Codigo);
    }
}
