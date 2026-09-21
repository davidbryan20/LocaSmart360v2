using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaSmart360.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaGeolocalizacaoVendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Fato_Vendas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Lon",
                table: "Fato_Vendas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Fato_Vendas");

            migrationBuilder.DropColumn(
                name: "Lon",
                table: "Fato_Vendas");
        }
    }
}
