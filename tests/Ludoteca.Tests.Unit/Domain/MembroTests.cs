using Ludoteca.Domain.Entities;
using Ludoteca.Domain.Exceptions;
using Ludoteca.Tests.Unit.Support;
using Xunit;

namespace Ludoteca.Tests.Unit.Domain;

public class MembroTests
{
    [Fact]
    public void Construtor_EmailComMaiusculas_NormalizaParaMinusculas()
    {
        // Arrange & Act
        var membro = new Membro("Maria Souza", "  Maria@Teste.COM ", null, Fabrica.Agora);

        // Assert
        Assert.Equal("maria@teste.com", membro.Email);
    }

    [Fact]
    public void Construtor_TelefoneEmBranco_GuardaNulo()
    {
        // Arrange & Act
        var membro = new Membro("Maria Souza", "maria@teste.com", "   ", Fabrica.Agora);

        // Assert
        Assert.Null(membro.Telefone);
    }

    [Fact]
    public void Construtor_NomeVazio_LancaRegraDeNegocio()
    {
        // Arrange & Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => new Membro(" ", "maria@teste.com", null, Fabrica.Agora));

        // Assert
        Assert.Equal("Membro.NomeObrigatorio", excecao.Codigo);
    }

    [Fact]
    public void DefinirEndereco_DadosCompletos_PreencheOEndereco()
    {
        // Arrange
        var membro = new Membro("Maria Souza", "maria@teste.com", null, Fabrica.Agora);

        // Act
        membro.DefinirEndereco("01001000", "Praça da Sé", "Sé", "São Paulo", "sp");

        // Assert
        Assert.Equal("01001000", membro.Cep);
        Assert.Equal("São Paulo", membro.Cidade);
        Assert.Equal("SP", membro.Uf);
    }

    [Fact]
    public void DefinirEndereco_LogradouroVazio_GuardaNulo()
    {
        // Arrange
        var membro = new Membro("Maria Souza", "maria@teste.com", null, Fabrica.Agora);

        // Act
        membro.DefinirEndereco("12345678", "", "", "Cidade Pequena", "MG");

        // Assert
        Assert.Null(membro.Logradouro);
        Assert.Null(membro.Bairro);
    }

    [Fact]
    public void DefinirEndereco_SemCidade_LancaRegraDeNegocio()
    {
        // Arrange
        var membro = new Membro("Maria Souza", "maria@teste.com", null, Fabrica.Agora);

        // Act
        var excecao = Assert.Throws<RegraDeNegocioException>(
            () => membro.DefinirEndereco("01001000", "Praça da Sé", "Sé", "", "SP"));

        // Assert
        Assert.Equal("Membro.EnderecoIncompleto", excecao.Codigo);
    }
}
