using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaSmart360.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarTabelaEtlLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtlLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataExecucao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegistrosProcessados = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MensagemErro = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtlLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtlLogs");
        }
    }
}
