using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Proyecto_Final.Data;

#nullable disable

namespace Proyecto_Final.Data.Migraciones;

[DbContext(typeof(Contexto))]
[Migration("20261007113000_PermitirEntregasPendientes")]
public partial class PermitirEntregasPendientes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "FechaEntrega",
            table: "Entregas",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE \"Entregas\" SET \"FechaEntrega\" = NOW() WHERE \"FechaEntrega\" IS NULL;");
        migrationBuilder.AlterColumn<DateTime>(
            name: "FechaEntrega",
            table: "Entregas",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);
    }
}
