using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Proyecto_Final.Data;

#nullable disable

namespace Proyecto_Final.Data.Migraciones;

[DbContext(typeof(Contexto))]
[Migration("20261007124500_EstadoPublicacionMaterial")]
public partial class EstadoPublicacionMaterialMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Estado",
            table: "Materiales",
            type: "integer",
            nullable: false,
            defaultValue: 1);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Estado", table: "Materiales");
    }
}
