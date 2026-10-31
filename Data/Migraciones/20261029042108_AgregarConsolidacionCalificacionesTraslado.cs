using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AgregarConsolidacionCalificacionesTraslado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EscalaMaximaConsolidada",
                table: "ResolucionesTrasladosAcademicos",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NotaConsolidadaAprobada",
                table: "ResolucionesTrasladosAcademicos",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NotaConsolidadaPropuesta",
                table: "ResolucionesTrasladosAcademicos",
                type: "numeric(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ResolucionesTrasladosAcademicos_Consolidacion",
                table: "ResolucionesTrasladosAcademicos",
                sql: "(\"NotaConsolidadaPropuesta\" IS NULL OR \"NotaConsolidadaPropuesta\" >= 0) AND (\"NotaConsolidadaAprobada\" IS NULL OR \"NotaConsolidadaAprobada\" >= 0) AND (\"EscalaMaximaConsolidada\" IS NULL OR \"EscalaMaximaConsolidada\" > 0) AND (\"Tipo\" = 3 OR (\"EscalaMaximaConsolidada\" IS NULL AND \"NotaConsolidadaPropuesta\" IS NULL AND \"NotaConsolidadaAprobada\" IS NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ResolucionesTrasladosAcademicos_Consolidacion",
                table: "ResolucionesTrasladosAcademicos");

            migrationBuilder.DropColumn(
                name: "EscalaMaximaConsolidada",
                table: "ResolucionesTrasladosAcademicos");

            migrationBuilder.DropColumn(
                name: "NotaConsolidadaAprobada",
                table: "ResolucionesTrasladosAcademicos");

            migrationBuilder.DropColumn(
                name: "NotaConsolidadaPropuesta",
                table: "ResolucionesTrasladosAcademicos");
        }
    }
}
