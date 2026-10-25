using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Abstractions.Security;
using Ludoteca.Application.Common;
using Ludoteca.Application.Dtos;
using Ludoteca.Application.Services;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Enums;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Tests.Unit.Support;
using Moq;
using Xunit;

namespace Ludoteca.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<ISenhaHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();

    private AuthService CriarServico() =>
        new(_usuarios.Object, _hasher.Object, _tokens.Object, new RelogioFixo(Fabrica.Agora));

    private static Usuario NovoAdministrador() =>
        new("Administrador", "admin@ludoteca.com.br", "hash-do-admin", PerfilUsuario.Administrador, Fabrica.Agora);

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaTokenEDadosDoUsuario()
    {
        // Arrange
        var usuario = NovoAdministrador();
        _usuarios
            .Setup(r => r.ObterPorEmailAsync("admin@ludoteca.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Usuario>.Success(usuario));
        _hasher.Setup(h => h.Verificar("hash-do-admin", "Admin@12345")).Returns(true);
        _tokens.Setup(t => t.Gerar(usuario)).Returns(new TokenGerado("token-jwt", Fabrica.Agora.AddHours(1)));
        var servico = CriarServico();

        // Act
        var resultado = await servico.LoginAsync(new LoginRequest { Email = "Admin@Ludoteca.com.br ", Senha = "Admin@12345" });

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("token-jwt", resultado.Data!.Token);
        Assert.Equal("Administrador", resultado.Data.Usuario.Perfil);
    }

    [Fact]
    public async Task LoginAsync_SenhaIncorreta_RetornaNaoAutorizado()
    {
        // Arrange
        _usuarios
            .Setup(r => r.ObterPorEmailAsync("admin@ludoteca.com.br", It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Usuario>.Success(NovoAdministrador()));
        _hasher.Setup(h => h.Verificar(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var servico = CriarServico();

        // Act
        var resultado = await servico.LoginAsync(new LoginRequest { Email = "admin@ludoteca.com.br", Senha = "errada" });

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.NaoAutorizado, resultado.Error!.Tipo);
        _tokens.Verify(t => t.Gerar(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UsuarioInexistente_RetornaMesmaMensagemDeSenhaIncorreta()
    {
        // Arrange
        _usuarios
            .Setup(r => r.ObterPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<Usuario>.Failure(Error.NaoEncontrado("Usuario.NaoEncontrado", "Usuário não encontrado.")));
        var servico = CriarServico();

        // Act
        var resultado = await servico.LoginAsync(new LoginRequest { Email = "ninguem@teste.com", Senha = "qualquer" });

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("Auth.CredenciaisInvalidas", resultado.Error!.Codigo);
    }

    [Fact]
    public async Task RegistrarAsync_EmailNovo_CriaUsuarioComPerfilAtendenteESenhaEmHash()
    {
        // Arrange
        Usuario? salvo = null;
        _hasher.Setup(h => h.GerarHash("Senha@123")).Returns("hash-gerado");
        _usuarios
            .Setup(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((usuario, _) => salvo = usuario)
            .Returns(Task.CompletedTask);
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(
            new RegistrarUsuarioRequest { Nome = "Carlos Atendente", Email = "carlos@teste.com", Senha = "Senha@123" });

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("Atendente", resultado.Data!.Perfil);
        Assert.NotNull(salvo);
        Assert.Equal(PerfilUsuario.Atendente, salvo!.Perfil);
        Assert.Equal("hash-gerado", salvo.SenhaHash);
    }

    [Fact]
    public async Task RegistrarAsync_EmailDuplicado_RetornaFalhaDeConflito()
    {
        // Arrange
        _hasher.Setup(h => h.GerarHash(It.IsAny<string>())).Returns("hash");
        _usuarios
            .Setup(r => r.AdicionarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflitoDeDadosException("Usuario.EmailDuplicado", "E-mail já cadastrado."));
        var servico = CriarServico();

        // Act
        var resultado = await servico.RegistrarAsync(
            new RegistrarUsuarioRequest { Nome = "Carlos Atendente", Email = "carlos@teste.com", Senha = "Senha@123" });

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoErro.Conflito, resultado.Error!.Tipo);
    }
}
