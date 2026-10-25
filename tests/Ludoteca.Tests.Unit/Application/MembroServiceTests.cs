using Ludoteca.Application.Abstractions.External;
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

public class MembroServiceTests
{
    private readonly Mock<IMembroRepository> _repositorio = new();
    private readonly Mock<ICepService> _cepService = new();

    private MembroService CriarServico() => new(_repositorio.Object, _cepService.Object, new RelogioFixo(Fabrica.Agora));

    private static MembroRequest RequestValido(string cep = "01001-000") => new()
    {
        Nome = "Maria Souza",
        Email = "maria@teste.com",
        Telefone = "11999990000",
        Cep = cep
    };

    private void ConfigurarCep(string cep, OperationResult<EnderecoResponse> resposta)
    {
        _cepService
            .Setup(c => c.ConsultarAsync(cep, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resposta);
    }

    private static OperationResult<EnderecoResponse> EnderecoDaSe() =>
        OperationResult<EnderecoResponse>.Success(new EnderecoResponse("01001000", "Praça da Sé", "Sé", "São Paulo", "SP"));

    [Fact]
    public async Task CriarAsync_CepValido_PreencheOEnderecoEPersiste()
    {
        // Arrange
        ConfigurarCep("01001000", EnderecoDaSe());
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("São Paulo", resultado.Data!.Endereco.Cidade);
        Assert.Equal("01001000", resultado.Data.Endereco.Cep);
        _repositorio.Verify(r => r.AdicionarAsync(It.IsAny<Membro>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_CepComFormatoInvalido_RetornaValidacaoSemConsultarOServicoExterno()
    {
        // Arrange
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(RequestValido("123"));

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Validacao, resultado.Error!.Tipo);
        _cepService.Verify(c => c.ConsultarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ServicoDeCepIndisponivel_RetornaFalhaDeServicoExterno()
    {
        // Arrange
        ConfigurarCep("01001000", OperationResult<EnderecoResponse>.Failure(
            Error.ServicoExterno("Endereco.ServicoIndisponivel", "Serviço indisponível.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.ServicoExterno, resultado.Error!.Tipo);
        _repositorio.Verify(r => r.AdicionarAsync(It.IsAny<Membro>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_CepInexistente_RetornaFalhaNaoEncontrado()
    {
        // Arrange
        ConfigurarCep("00000000", OperationResult<EnderecoResponse>.Failure(
            Error.NaoEncontrado("Endereco.CepNaoEncontrado", "CEP não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(RequestValido("00000-000"));

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task CriarAsync_EmailDuplicado_RetornaFalhaDeConflito()
    {
        // Arrange
        ConfigurarCep("01001000", EnderecoDaSe());
        _repositorio
            .Setup(r => r.AdicionarAsync(It.IsAny<Membro>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflitoDeDadosException("Membro.EmailDuplicado", "E-mail já cadastrado."));
        var servico = CriarServico();

        // Act
        var resultado = await servico.CriarAsync(RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Membro.EmailDuplicado", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task AtualizarAsync_MesmoCep_NaoConsultaOServicoExterno()
    {
        // Arrange
        var membro = Fabrica.NovoMembro();
        _repositorio
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Membro>.Success(membro));
        var request = RequestValido("01001-000");
        request.Nome = "Maria Souza Lima";
        var servico = CriarServico();

        // Act
        var resultado = await servico.AtualizarAsync(1, request);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Maria Souza Lima", membro.Nome);
        _cepService.Verify(c => c.ConsultarAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositorio.Verify(r => r.AtualizarAsync(membro, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_CepDiferente_ConsultaOServicoEAtualizaOEndereco()
    {
        // Arrange
        var membro = Fabrica.NovoMembro();
        _repositorio
            .Setup(r => r.ObterRastreadoPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Membro>.Success(membro));
        ConfigurarCep("20040020", OperationResult<EnderecoResponse>.Success(
            new EnderecoResponse("20040020", "Rua da Assembleia", "Centro", "Rio de Janeiro", "RJ")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.AtualizarAsync(1, RequestValido("20040-020"));

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Rio de Janeiro", membro.Cidade);
        Assert.Equal("RJ", membro.Uf);
    }

    [Fact]
    public async Task AtualizarAsync_MembroInexistente_RetornaFalhaNaoEncontrado()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterRastreadoPorIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Membro>.Failure(Error.NaoEncontrado("Membro.NaoEncontrado", "Membro 99 não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.AtualizarAsync(99, RequestValido());

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.NaoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task RemoverAsync_MembroComEmprestimos_RetornaFalhaDeConflito()
    {
        // Arrange
        _repositorio
            .Setup(r => r.RemoverAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflitoDeDadosException("Membro.PossuiEmprestimos", "Membro possui empréstimos."));
        var servico = CriarServico();

        // Act
        var resultado = await servico.RemoverAsync(1);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Conflito, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task ObterAsync_MembroExistente_RetornaOMembroComEndereco()
    {
        // Arrange
        _repositorio
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Membro>.Success(Fabrica.NovoMembro()));
        var servico = CriarServico();

        // Act
        var resultado = await servico.ObterAsync(1);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Maria Souza", resultado.Data!.Nome);
        Assert.Equal("SP", resultado.Data.Endereco.Uf);
    }
}
