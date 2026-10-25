using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Enums;
using Ludoteca.Infrastructure.Security;
using Ludoteca.Tests.Unit.Support;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ludoteca.Tests.Unit.Infrastructure;

public class JwtTokenServiceTests
{
    private static JwtTokenService CriarServico() =>
        new(
            Options.Create(new JwtOptions
            {
                Chave = new string('k', 48),
                Emissor = "Ludoteca.Teste",
                Audiencia = "Ludoteca.Clientes",
                ExpiraEmMinutos = 30
            }),
            new RelogioFixo(Fabrica.Agora));

    [Fact]
    public void Gerar_UsuarioAdministrador_IncluiClaimsDeEmailENomeEPerfil()
    {
        // Arrange
        var usuario = new Usuario("Administrador", "admin@ludoteca.com.br", "hash", PerfilUsuario.Administrador, Fabrica.Agora);
        var servico = CriarServico();

        // Act
        var gerado = servico.Gerar(usuario);

        // Assert
        var token = new JwtSecurityTokenHandler().ReadJwtToken(gerado.Token);
        Assert.Equal("Ludoteca.Teste", token.Issuer);
        Assert.Contains("Ludoteca.Clientes", token.Audiences);
        Assert.Equal("admin@ludoteca.com.br", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Administrador", token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("Administrador", token.Claims.First(c => c.Type == ClaimTypes.Name).Value);
    }

    [Fact]
    public void Gerar_ConfiguracaoDe30Minutos_ExpiraEm30MinutosAPartirDoRelogio()
    {
        // Arrange
        var usuario = new Usuario("Carlos", "carlos@teste.com", "hash", PerfilUsuario.Atendente, Fabrica.Agora);
        var servico = CriarServico();

        // Act
        var gerado = servico.Gerar(usuario);

        // Assert
        Assert.Equal(Fabrica.Agora.AddMinutes(30), gerado.ExpiraEm);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(gerado.Token);
        Assert.Equal("Atendente", token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }
}

public class SenhaHasherTests
{
    [Fact]
    public void GerarHash_Senha_RetornaHashDiferenteDaSenha()
    {
        // Arrange
        var hasher = new SenhaHasher();

        // Act
        var hash = hasher.GerarHash("Senha@123");

        // Assert
        Assert.NotEqual("Senha@123", hash);
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Verificar_SenhaCorreta_RetornaVerdadeiro()
    {
        // Arrange
        var hasher = new SenhaHasher();
        var hash = hasher.GerarHash("Senha@123");

        // Act
        var valido = hasher.Verificar(hash, "Senha@123");

        // Assert
        Assert.True(valido);
    }

    [Fact]
    public void Verificar_SenhaIncorreta_RetornaFalso()
    {
        // Arrange
        var hasher = new SenhaHasher();
        var hash = hasher.GerarHash("Senha@123");

        // Act
        var valido = hasher.Verificar(hash, "outra-senha");

        // Assert
        Assert.False(valido);
    }

    [Fact]
    public void GerarHash_MesmaSenhaDuasVezes_GeraHashesDiferentesPorCausaDoSalt()
    {
        // Arrange
        var hasher = new SenhaHasher();

        // Act
        var primeiro = hasher.GerarHash("Senha@123");
        var segundo = hasher.GerarHash("Senha@123");

        // Assert
        Assert.NotEqual(primeiro, segundo);
    }
}
