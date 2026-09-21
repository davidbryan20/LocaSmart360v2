using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaSmart360.Migrations
{
    /// <inheritdoc />
    public partial class Inicial_LocaSmart360 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dim_Produtos",
                columns: table => new
                {
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nome = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PalavrasChaveSEO = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dim_Produtos", x => x.ProdutoId);
                });

            migrationBuilder.CreateTable(
                name: "Dim_Usuarios",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SenhaHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeLoja = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dim_Usuarios", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "Fato_Vendas",
                columns: table => new
                {
                    VendaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ScoreRiscoFraude = table.Column<int>(type: "integer", nullable: false),
                    DataVenda = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fato_Vendas", x => x.VendaId);
                    table.ForeignKey(
                        name: "FK_Fato_Vendas_Dim_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Dim_Produtos",
                        principalColumn: "ProdutoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dim_Usuarios_Email",
                table: "Dim_Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fato_Vendas_ProdutoId",
                table: "Fato_Vendas",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dim_Usuarios");

            migrationBuilder.DropTable(
                name: "Fato_Vendas");

            migrationBuilder.DropTable(
                name: "Dim_Produtos");
        }
    }
}
