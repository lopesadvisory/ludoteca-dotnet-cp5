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

public class JogoServiceTests
{
    private readonly Mock<IJogoRepository> _repositorio = new();

    private JogoService CriarServico() => new(_repositorio.Object, new RelogioFixo(Fabrica.Agora));

    private static JogoRequest RequestValido() => new()
    {
        Titulo = "Catan",
        Editora = "Devir",
        Categoria = "Estratégia",
        MinJogadores = 3,
        MaxJogadores = 4,
        DuracaoMediaMinutos = 90
    };

    [Fact]
    public async Task CriarAsync_DadosValidos_RetornaSucessoComJogoDisponivel()
    {
        // Arrange
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Catan", resultado.Data!.Titulo);
        Assert.True(resultado.Data.Disponivel);
        _repositorio.Verify(r => r.AdicionarAsync(It.IsAny<Jogo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_RepositorioLancaConflito_RetornaFalhaDeConflito()
    {
        // Arrange
        _repositorio
            .Setup(r => r.AdicionarAsync(It.IsAny<Jogo>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflitoDeDadosException("Jogo.TituloDuplicado", "Título já cadastrado."));
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Conflito, resultado.Error!.Tipo);
        Assert.Equal("Jogo.TituloDuplicado", resultado.Error.Codigo);
    }

    [Fact]
    public async Task CriarAsync_MaximoMenorQueMinimo_RetornaFalhaSemChamarORepositorio()
    {
        // Arrange
        var request = RequestValido();
        request.MinJogadores = 5;
        request.MaxJogadores = 2;
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(request);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.RegraDeNegocio, resultado.Error!.Tipo);
        _repositorio.Verify(r => r.AdicionarAsync(It.IsAny<Jogo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ObterAsync_JogoExistente_RetornaOJogo()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Jogo>.Success(Fabrica.NovoJogo()));
        var servico = CriarServico();

        // Act
        var resultado = await servico.ObterAsync(1);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Catan", resultado.Data!.Titulo);
    }

    [Fact]
    public async Task ObterAsync_JogoInexistente_RetornaFalhaNaoEncontrado()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterPorIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Jogo>.Failure(Error.NaoEncontrado("Jogo.NaoEncontrado", "Jogo 99 não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.ObterAsync(99);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task AtualizarAsync_JogoExistente_AplicaAsAlteracoesEPersiste()
    {
        // Arrange
        var jogo = Fabrica.NovoJogo();
        _repositorio
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Jogo>.Success(jogo));
        var request = RequestValido();
        request.Titulo = "Catan Edição Especial";
        var servico = CriarServico();

        // Act
        var resultado = await servico.AtualizarAsync(1, request);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Catan Edição Especial", jogo.Titulo);
        _repositorio.Verify(r => r.AtualizarAsync(jogo, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_JogoInexistente_RetornaFalhaSemPersistir()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterRastreadoPorIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Jogo>.Failure(Error.NaoEncontrado("Jogo.NaoEncontrado", "Jogo 99 não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.AtualizarAsync(99, RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        _repositorio.Verify(r => r.AtualizarAsync(It.IsAny<Jogo>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoverAsync_RepositorioLancaNaoEncontrado_RetornaFalha()
    {
        // Arrange
        _repositorio
            .Setup(r => r.RemoverAsync(99, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntidadeNaoEncontradaException("Jogo.NaoEncontrado", "Jogo 99 não encontrado."));
        var servico = CriarServico();

        // Act
        var resultado = await servico.RemoverAsync(99);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task RemoverAsync_RepositorioLancaConflito_RetornaFalhaDeConflito()
    {
        // Arrange
        _repositorio
            .Setup(r => r.RemoverAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflitoDeDadosException("Jogo.PossuiEmprestimos", "Jogo possui empréstimos."));
        var servico = CriarServico();

        // Act
        var resultado = await servico.RemoverAsync(1);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Jogo.PossuiEmprestimos", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task RemoverAsync_JogoExistente_RetornaSucesso()
    {
        // Arrange
        var servico = CriarServico();

        // Act
        var resultado = await servico.RemoverAsync(1);

        // Assert
        Assert.True(resultado.IsSuccess);
        _repositorio.Verify(r => r.RemoverAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_PaginaComJogos_ConverteParaResponseMantendoMetadados()
    {
        // Arrange
        var pagina = new PageResult<Jogo>(new[] { Fabrica.NovoJogo("Catan"), Fabrica.NovoJogo("Azul") }, 1, 2, 5);
        _repositorio
            .Setup(r => r.ListarAsync(It.IsAny<JogoFiltro>(), It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagina);
        var servico = CriarServico();

        // Act
        var resultado = await servico.ListarAsync(new JogoFiltro(null, null, null), new PageRequest(1, 2));

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(2, resultado.Data!.ItemCount);
        Assert.Equal(3, resultado.Data.TotalPages);
        Assert.Equal("Catan", resultado.Data.Data[0].Titulo);
    }
}
