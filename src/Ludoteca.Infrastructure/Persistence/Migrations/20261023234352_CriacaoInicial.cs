using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ludoteca.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LUDO_JOGO",
                columns: table => new
                {
                    JOGO_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TITULO = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    EDITORA = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: false),
                    CATEGORIA = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: false),
                    MIN_JOGADORES = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MAX_JOGADORES = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DURACAO_MEDIA_MIN = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DISPONIVEL = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CRIADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LUDO_JOGO", x => x.JOGO_ID);
                });

            migrationBuilder.CreateTable(
                name: "LUDO_MEMBRO",
                columns: table => new
                {
                    MEMBRO_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(160)", maxLength: 160, nullable: false),
                    TELEFONE = table.Column<string>(type: "NVARCHAR2(25)", maxLength: 25, nullable: true),
                    CEP = table.Column<string>(type: "NVARCHAR2(8)", maxLength: 8, nullable: false),
                    LOGRADOURO = table.Column<string>(type: "NVARCHAR2(160)", maxLength: 160, nullable: true),
                    BAIRRO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    CIDADE = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    UF = table.Column<string>(type: "NVARCHAR2(2)", maxLength: 2, nullable: false),
                    CRIADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LUDO_MEMBRO", x => x.MEMBRO_ID);
                });

            migrationBuilder.CreateTable(
                name: "LUDO_USUARIO",
                columns: table => new
                {
                    USUARIO_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(160)", maxLength: 160, nullable: false),
                    SENHA_HASH = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    PERFIL = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CRIADO_EM = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LUDO_USUARIO", x => x.USUARIO_ID);
                });

            migrationBuilder.CreateTable(
                name: "LUDO_EMPRESTIMO",
                columns: table => new
                {
                    EMPRESTIMO_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    JOGO_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MEMBRO_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DATA_EMPRESTIMO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DATA_PREVISTA_DEVOLUCAO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DATA_DEVOLUCAO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LUDO_EMPRESTIMO", x => x.EMPRESTIMO_ID);
                    table.ForeignKey(
                        name: "FK_LUDO_EMPRESTIMO_LUDO_JOGO_JOGO_ID",
                        column: x => x.JOGO_ID,
                        principalTable: "LUDO_JOGO",
                        principalColumn: "JOGO_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LUDO_EMPRESTIMO_LUDO_MEMBRO_MEMBRO_ID",
                        column: x => x.MEMBRO_ID,
                        principalTable: "LUDO_MEMBRO",
                        principalColumn: "MEMBRO_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LUDO_EMPRESTIMO_JOGO_DEV",
                table: "LUDO_EMPRESTIMO",
                columns: new[] { "JOGO_ID", "DATA_DEVOLUCAO" });

            migrationBuilder.CreateIndex(
                name: "IX_LUDO_EMPRESTIMO_MEMBRO_DEV",
                table: "LUDO_EMPRESTIMO",
                columns: new[] { "MEMBRO_ID", "DATA_DEVOLUCAO" });

            migrationBuilder.CreateIndex(
                name: "IX_LUDO_JOGO_CATEGORIA",
                table: "LUDO_JOGO",
                column: "CATEGORIA");

            migrationBuilder.CreateIndex(
                name: "UX_LUDO_JOGO_TITULO",
                table: "LUDO_JOGO",
                column: "TITULO",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LUDO_MEMBRO_NOME",
                table: "LUDO_MEMBRO",
                column: "NOME");

            migrationBuilder.CreateIndex(
                name: "UX_LUDO_MEMBRO_EMAIL",
                table: "LUDO_MEMBRO",
                column: "EMAIL",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LUDO_USUARIO_EMAIL",
                table: "LUDO_USUARIO",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LUDO_EMPRESTIMO");

            migrationBuilder.DropTable(
                name: "LUDO_USUARIO");

            migrationBuilder.DropTable(
                name: "LUDO_JOGO");

            migrationBuilder.DropTable(
                name: "LUDO_MEMBRO");
        }
    }
}
