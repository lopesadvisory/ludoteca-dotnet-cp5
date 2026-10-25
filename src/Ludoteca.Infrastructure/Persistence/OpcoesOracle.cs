using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Ludoteca.Infrastructure.Persistence;

internal static class OpcoesOracle
{
    public const string TabelaDeMigrations = "LUDO_MIGRATIONS_HISTORY";

    // Compatibilidade com Oracle 19c: sem ela o provider mapeia bool para BOOLEAN, que só existe no 23ai.
    public static void Configurar(OracleDbContextOptionsBuilder oracle)
    {
        oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
        oracle.MigrationsHistoryTable(TabelaDeMigrations);
    }
}
