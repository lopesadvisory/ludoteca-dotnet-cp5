using Ludoteca.Application.Abstractions.Repositories;
using Ludoteca.Application.Dtos;
using Ludoteca.Domain.Entities;

namespace Ludoteca.Application;

public static class Mapeamentos
{
    public const string StatusAtivo = "Ativo";
    public const string StatusAtrasado = "Atrasado";
    public const string StatusDevolvido = "Devolvido";

    public static JogoResponse ParaResponse(this Jogo jogo)
    {
        return new JogoResponse(
            jogo.Id,
            jogo.Titulo,
            jogo.Editora,
            jogo.Categoria,
            jogo.MinJogadores,
            jogo.MaxJogadores,
            jogo.DuracaoMediaMinutos,
            jogo.Disponivel,
            jogo.CriadoEm);
    }

    public static MembroResponse ParaResponse(this Membro membro)
    {
        var endereco = new EnderecoResponse(membro.Cep, membro.Logradouro, membro.Bairro, membro.Cidade, membro.Uf);
        return new MembroResponse(membro.Id, membro.Nome, membro.Email, membro.Telefone, endereco, membro.CriadoEm);
    }

    public static UsuarioResponse ParaResponse(this Usuario usuario)
    {
        return new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil.ToString());
    }

    public static EmprestimoResponse ParaResponse(this Emprestimo emprestimo, DateTime agora)
    {
        return new EmprestimoResponse(
            emprestimo.Id,
            emprestimo.JogoId,
            emprestimo.Jogo.Titulo,
            emprestimo.MembroId,
            emprestimo.Membro.Nome,
            emprestimo.DataEmprestimo,
            emprestimo.DataPrevistaDevolucao,
            emprestimo.DataDevolucao,
            CalcularStatus(emprestimo.Ativo, emprestimo.EstaAtrasado(agora)));
    }

    public static EmprestimoResponse ParaResponse(this EmprestimoDetalhe detalhe, DateTime agora)
    {
        return new EmprestimoResponse(
            detalhe.Id,
            detalhe.JogoId,
            detalhe.JogoTitulo,
            detalhe.MembroId,
            detalhe.MembroNome,
            detalhe.DataEmprestimo,
            detalhe.DataPrevistaDevolucao,
            detalhe.DataDevolucao,
            CalcularStatus(detalhe.Ativo, detalhe.EstaAtrasado(agora)));
    }

    private static string CalcularStatus(bool ativo, bool atrasado)
    {
        if (!ativo)
            return StatusDevolvido;

        return atrasado ? StatusAtrasado : StatusAtivo;
    }
}
