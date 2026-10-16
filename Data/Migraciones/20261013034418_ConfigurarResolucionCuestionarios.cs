using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Final.Data.Migraciones
{
    /// <inheritdoc />
    public partial class ConfigurarResolucionCuestionarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Retroalimentacion",
                table: "Preguntas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLimite",
                table: "IntentosCuestionarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrdenPreguntas",
                table: "IntentosCuestionarios",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PreguntaActual",
                table: "IntentosCuestionarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "OrdenAleatorio",
                table: "Cuestionarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PermitirNavegacion",
                table: "Cuestionarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TiempoPorPreguntaSegundos",
                table: "Cuestionarios",
                type: "integer",
                nullable: true);

            // Los intentos anteriores a esta migración se presentaron en el orden
            // definido por el docente. Se registra ese orden para poder mostrar
            // su revisión de respuestas.
            migrationBuilder.Sql("""
                UPDATE "IntentosCuestionarios" AS i
                SET "OrdenPreguntas" = COALESCE((
                    SELECT string_agg(p."Id"::text, ',' ORDER BY p."Orden")
                    FROM "Preguntas" AS p
                    WHERE p."CuestionarioId" = i."CuestionarioId"), '')
                WHERE i."OrdenPreguntas" = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_IntentosCuestionarios_IntentoEnCurso",
                table: "IntentosCuestionarios",
                columns: new[] { "CuestionarioId", "InscripcionId" },
                unique: true,
                filter: "\"Estado\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IntentosCuestionarios_IntentoEnCurso",
                table: "IntentosCuestionarios");

            migrationBuilder.DropColumn(
                name: "Retroalimentacion",
                table: "Preguntas");

            migrationBuilder.DropColumn(
                name: "FechaLimite",
                table: "IntentosCuestionarios");

            migrationBuilder.DropColumn(
                name: "OrdenPreguntas",
                table: "IntentosCuestionarios");

            migrationBuilder.DropColumn(
                name: "PreguntaActual",
                table: "IntentosCuestionarios");

            migrationBuilder.DropColumn(
                name: "OrdenAleatorio",
                table: "Cuestionarios");

            migrationBuilder.DropColumn(
                name: "PermitirNavegacion",
                table: "Cuestionarios");

            migrationBuilder.DropColumn(
                name: "TiempoPorPreguntaSegundos",
                table: "Cuestionarios");
        }
    }
}
