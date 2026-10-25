using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace Ludoteca.Infrastructure.Persistence;

internal static class ErrosDeBanco
{
    // ORA-00001: violação de índice único.
    public static bool EhViolacaoDeUnicidade(DbUpdateException excecao)
    {
        return excecao.InnerException is OracleException { Number: 1 };
    }

    // ORA-02292: existem registros filhos ligados ao registro removido.
    public static bool EhViolacaoDeIntegridade(DbUpdateException excecao)
    {
        return excecao.InnerException is OracleException { Number: 2292 };
    }
}
