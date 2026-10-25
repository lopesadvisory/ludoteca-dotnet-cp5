using Ludoteca.Application.Abstractions.External;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Application.Services;
using Ludoteca.Domain.Exceptions;
using Moq;
using Xunit;

namespace Ludoteca.Tests.Unit.Application;

public class EnderecoServiceTests
{
    private readonly Mock<ICepService> _cepService = new();

    [Fact]
    public async Task ConsultarPorCepAsync_CepComMascara_ConsultaOServicoComOCepNormalizado()
    {
        // Arrange
        var endereco = new EnderecoResponse("01001000", "Praça da Sé", "Sé", "São Paulo", "SP");
        _cepService
            .Setup(c => c.ConsultarAsync("01001000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<EnderecoResponse>.Success(endereco));
        var servico = new EnderecoService(_cepService.Object);

        // Act
        var resultado = await servico.ConsultarPorCepAsync("01001-000");

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("São Paulo", resultado.Data!.Cidade);
    }

    [Fact]
    public async Task ConsultarPorCepAsync_CepInvalido_RetornaValidacaoSemChamarOServico()
    {
        // Arrange
        var servico = new EnderecoService(_cepService.Object);

        // Act
        var resultado = await servico.ConsultarPorCepAsync("abc");

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Validacao, resultado.Error!.Tipo);
        _cepService.Verify(c => c.ConsultarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
