using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarActivoARoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "AspNetRoles",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "AspNetRoles");
        }
    }
}
