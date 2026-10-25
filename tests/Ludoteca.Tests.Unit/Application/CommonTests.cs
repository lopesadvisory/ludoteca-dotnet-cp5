using Ludoteca.Application.Common;
using Ludoteca.Domain.Exceptions;
using Xunit;

namespace Ludoteca.Tests.Unit.Application;

public class OperationResultTests
{
    [Fact]
    public void Success_ComDados_MarcaSucessoEGuardaOsDados()
    {
        // Arrange & Act
        var resultado = OperationResult<string>.Success("ok");

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.False(resultado.IsFailure);
        Assert.Equal("ok", resultado.Data);
        Assert.Null(resultado.Error);
    }

    [Fact]
    public void Failure_ComErro_MarcaFalhaEGuardaOErro()
    {
        // Arrange
        var erro = Error.NaoEncontrado("Jogo.NaoEncontrado", "Jogo 1 não encontrado.");

        // Act
        var resultado = OperationResult<string>.Failure(erro);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Null(resultado.Data);
        Assert.Equal(erro, resultado.Error);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public void SuccessSemDados_ResultadoSimples_NaoTemErro()
    {
        // Arrange & Act
        var resultado = OperationResult.Success();

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Null(resultado.Error);
    }

    [Fact]
    public void De_ExcecaoDeDominio_CopiaCodigoMensagemETipo()
    {
        // Arrange
        var excecao = new ConflitoDeDadosException("Jogo.TituloDuplicado", "Título já cadastrado.");

        // Act
        var erro = Error.De(excecao);

        // Assert
        Assert.Equal("Jogo.TituloDuplicado", erro.Codigo);
        Assert.Equal("Título já cadastrado.", erro.Mensagem);
        Assert.Equal(TipoErro.Conflito, erro.Tipo);
    }
}

public class PageResultTests
{
    [Fact]
    public void TotalPages_ArredondaParaCima()
    {
        // Arrange & Act
        var pagina = new PageResult<int>(new[] { 1, 2 }, 1, 2, 5);

        // Assert
        Assert.Equal(3, pagina.TotalPages);
        Assert.Equal(2, pagina.ItemCount);
        Assert.Equal(5, pagina.TotalRecords);
    }

    [Fact]
    public void TotalPages_SemRegistros_RetornaZero()
    {
        // Arrange & Act
        var pagina = new PageResult<int>(Array.Empty<int>(), 1, 10, 0);

        // Assert
        Assert.Equal(0, pagina.TotalPages);
        Assert.False(pagina.HasNext);
        Assert.False(pagina.HasPrevious);
    }

    [Fact]
    public void HasNextEHasPrevious_PaginaDoMeio_SaoVerdadeiros()
    {
        // Arrange & Act
        var pagina = new PageResult<int>(new[] { 3, 4 }, 2, 2, 6);

        // Assert
        Assert.True(pagina.HasPrevious);
        Assert.True(pagina.HasNext);
    }

    [Fact]
    public void HasNext_UltimaPagina_RetornaFalso()
    {
        // Arrange & Act
        var pagina = new PageResult<int>(new[] { 5 }, 3, 2, 5);

        // Assert
        Assert.False(pagina.HasNext);
        Assert.True(pagina.HasPrevious);
    }

    [Fact]
    public void Converter_AplicaOConversorMantendoOsMetadados()
    {
        // Arrange
        var pagina = new PageResult<int>(new[] { 1, 2 }, 2, 2, 6);

        // Act
        var convertida = pagina.Converter(numero => $"n{numero}");

        // Assert
        Assert.Equal(new[] { "n1", "n2" }, convertida.Data);
        Assert.Equal(2, convertida.Page);
        Assert.Equal(6, convertida.TotalRecords);
    }
}

public class PageRequestTests
{
    [Theory]
    [InlineData(0, 10, 1, 10)]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(3, 0, 3, PageRequest.TamanhoPadrao)]
    [InlineData(2, 500, 2, PageRequest.TamanhoMaximo)]
    public void Construtor_ValoresForaDoLimite_SaoNormalizados(int page, int pageSize, int paginaEsperada, int tamanhoEsperado)
    {
        // Arrange & Act
        var requisicao = new PageRequest(page, pageSize);

        // Assert
        Assert.Equal(paginaEsperada, requisicao.Page);
        Assert.Equal(tamanhoEsperado, requisicao.PageSize);
    }

    [Fact]
    public void Skip_SegundaPagina_PulaOsItensDaPrimeira()
    {
        // Arrange & Act
        var requisicao = new PageRequest(3, 10);

        // Assert
        Assert.Equal(20, requisicao.Skip);
    }
}

public class CepTests
{
    [Theory]
    [InlineData("01001-000", "01001000")]
    [InlineData("01001000", "01001000")]
    [InlineData(" 01001 000 ", "01001000")]
    public void Normalizar_CepComOitoDigitos_RetornaApenasDigitos(string entrada, string esperado)
    {
        // Arrange & Act
        var cep = Cep.Normalizar(entrada);

        // Assert
        Assert.Equal(esperado, cep);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefgh")]
    [InlineData("0100100012")]
    public void Normalizar_CepInvalido_RetornaNulo(string? entrada)
    {
        // Arrange & Act
        var cep = Cep.Normalizar(entrada);

        // Assert
        Assert.Null(cep);
    }
}
