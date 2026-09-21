using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaSmart360.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCargoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cargo",
                table: "Dim_Usuarios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cargo",
                table: "Dim_Usuarios");
        }
    }
}
