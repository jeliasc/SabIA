using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class ImplementarCerosAdministrativos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRectificacionAdministrativa",
                table: "Entregas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRectificacionAdministrativa",
                table: "Entregas",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrigenCalificacionAdministrativa",
                table: "Entregas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RectificadoPorUsuarioId",
                table: "Entregas",
                type: "text",
                nullable: true);

            // La firma coincide exclusivamente con el flujo heredado de
            // "calificar con cero": entrega sin fecha, nota cero y estado
            // calificado. Los demás casos quedan sin reclasificar.
            migrationBuilder.Sql(
                "UPDATE \"Entregas\" SET \"OrigenCalificacionAdministrativa\" = 1 " +
                "WHERE \"Estado\" = 2 AND \"Calificacion\" = 0 " +
                "AND \"FechaEntrega\" IS NULL " +
                "AND \"OrigenCalificacionAdministrativa\" IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_OrigenCalificacionAdministrativa",
                table: "Entregas",
                column: "OrigenCalificacionAdministrativa");

            migrationBuilder.CreateIndex(
                name: "IX_Entregas_RectificadoPorUsuarioId",
                table: "Entregas",
                column: "RectificadoPorUsuarioId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Entregas_CalificacionAdministrativa",
                table: "Entregas",
            sql: "\"OrigenCalificacionAdministrativa\" IS NULL OR (\"OrigenCalificacionAdministrativa\" IN (1, 2) AND \"FechaEntrega\" IS NULL AND ((\"Calificacion\" = 0 AND \"Estado\" = 2) OR (\"Calificacion\" IS NULL AND \"Estado\" = 6 AND \"FechaRectificacionAdministrativa\" IS NOT NULL AND \"MotivoRectificacionAdministrativa\" IS NOT NULL AND \"RectificadoPorUsuarioId\" IS NOT NULL)))");

            migrationBuilder.AddForeignKey(
                name: "FK_Entregas_AspNetUsers_RectificadoPorUsuarioId",
                table: "Entregas",
                column: "RectificadoPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entregas_AspNetUsers_RectificadoPorUsuarioId",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_OrigenCalificacionAdministrativa",
                table: "Entregas");

            migrationBuilder.DropIndex(
                name: "IX_Entregas_RectificadoPorUsuarioId",
                table: "Entregas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Entregas_CalificacionAdministrativa",
                table: "Entregas");

            migrationBuilder.DropColumn(
                name: "FechaRectificacionAdministrativa",
                table: "Entregas");

            migrationBuilder.DropColumn(
                name: "MotivoRectificacionAdministrativa",
                table: "Entregas");

            migrationBuilder.DropColumn(
                name: "OrigenCalificacionAdministrativa",
                table: "Entregas");

            migrationBuilder.DropColumn(
                name: "RectificadoPorUsuarioId",
                table: "Entregas");
        }
    }
}
