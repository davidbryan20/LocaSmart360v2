using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaSmart360.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIpCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpCliente",
                table: "Fato_Vendas",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpCliente",
                table: "Fato_Vendas");
        }
    }
}
